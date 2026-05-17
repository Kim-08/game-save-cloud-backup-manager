using System.Diagnostics;
using GameSaveCloudBackup.Models;
using GameSaveCloudBackup.Services;

namespace GameSaveCloudBackup.UI;

public sealed class MainForm : Form
{
    private const string BackupColumnName = "BackupNow";
    private const string RestoreColumnName = "RestoreFromCloud";
    private const string HistoryColumnName = "BackupHistory";
    private const string OpenSaveFolderColumnName = "OpenSaveFolder";

    private readonly ConfigService _configService;
    private readonly LoggingService _loggingService;
    private readonly RcloneService _rcloneService;
    private readonly BackupService _backupService;
    private readonly GameMonitorService _gameMonitorService;
    private readonly AppConfig _appConfig;
    private readonly DataGridView _gamesGrid = new();
    private readonly TextBox _logsTextBox = new();
    private readonly Label _emptyStateLabel = new();
    private readonly Label _rcloneStatusLabel = new();
    private readonly CancellationTokenSource _appCts = new();
    private readonly HashSet<Guid> _startupRestoreCheckedGameIds = [];
    private readonly HashSet<Guid> _startupRestorePromptedGameIds = [];
    private IReadOnlyList<string> _rcloneRemotes = [];

    public MainForm(
        ConfigService configService,
        LoggingService loggingService,
        RcloneService rcloneService,
        BackupService backupService,
        GameMonitorService gameMonitorService,
        AppConfig appConfig)
    {
        _configService = configService;
        _loggingService = loggingService;
        _rcloneService = rcloneService;
        _backupService = backupService;
        _gameMonitorService = gameMonitorService;
        _appConfig = appConfig;
        _gameMonitorService.StateChanged += GameMonitorServiceStateChanged;

        Text = "Game Save Cloud Backup Manager";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1320;
        Height = 820;
        MinimumSize = new Size(1100, 700);

        BuildLayout();
        RefreshGameList();
        RefreshLogs();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        try
        {
            await RefreshRcloneStatusAsync();
            await CheckStartupRestorePromptsAsync();
            _gameMonitorService.Start(_appConfig.Games);
        }
        catch (Exception ex)
        {
            _loggingService.Error("Startup work failed", ex);
            ShowError("Startup", "Some startup checks failed. The app is still open, because apparently we are being merciful today.", ex);
            RefreshLogs();
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _appCts.Cancel();
        _gameMonitorService.Stop();
        base.OnFormClosing(e);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _gameMonitorService.StateChanged -= GameMonitorServiceStateChanged;
            _appCts.Dispose();
        }

        base.Dispose(disposing);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            RowCount = 4,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 64));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 36));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        header.Controls.Add(new Label
        {
            Text = "Game Save Cloud Backup Manager",
            Font = new Font(Font.FontFamily, 18, FontStyle.Bold),
            AutoSize = true,
            Anchor = AnchorStyles.Left
        }, 0, 0);
        var addButton = new Button { Text = "Add Game", Dock = DockStyle.Fill };
        addButton.Click += AddGame;
        header.Controls.Add(addButton, 1, 0);
        root.Controls.Add(header, 0, 0);

        var rclonePanel = new GroupBox { Text = "Rclone Status", Dock = DockStyle.Fill };
        var rcloneLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 4 };
        rcloneLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rcloneLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155));
        rcloneLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        rcloneLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160));
        _rcloneStatusLabel.Text = "Checking rclone availability...";
        _rcloneStatusLabel.Dock = DockStyle.Fill;
        _rcloneStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _rcloneStatusLabel.Padding = new Padding(10, 0, 0, 0);
        var configureRcloneButton = new Button { Text = "Configure Rclone", Dock = DockStyle.Fill };
        configureRcloneButton.Click += (_, _) => ConfigureRclone();
        var refreshRcloneButton = new Button { Text = "Refresh Rclone", Dock = DockStyle.Fill };
        refreshRcloneButton.Click += async (_, _) => await SafeUiAsync("Refresh Rclone", RefreshRcloneStatusAsync);
        var rcloneHelpButton = new Button { Text = "Rclone Setup Help", Dock = DockStyle.Fill };
        rcloneHelpButton.Click += (_, _) => ShowRcloneHelp();
        rcloneLayout.Controls.Add(_rcloneStatusLabel, 0, 0);
        rcloneLayout.Controls.Add(configureRcloneButton, 1, 0);
        rcloneLayout.Controls.Add(refreshRcloneButton, 2, 0);
        rcloneLayout.Controls.Add(rcloneHelpButton, 3, 0);
        rclonePanel.Controls.Add(rcloneLayout);
        root.Controls.Add(rclonePanel, 0, 1);

        var gamePanel = new GroupBox { Text = "Games", Dock = DockStyle.Fill };
        var gamePanelLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        gamePanelLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        gamePanelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        ConfigureGamesGrid();

        var gridHost = new Panel { Dock = DockStyle.Fill };
        _emptyStateLabel.Text =
            "No games added yet. Click Add Game to pick a game EXE, save folder, rclone remote, " +
            "and cloud backup folder. A small ritual, then backups.";
        _emptyStateLabel.Dock = DockStyle.Fill;
        _emptyStateLabel.TextAlign = ContentAlignment.MiddleCenter;
        _emptyStateLabel.Font = new Font(Font.FontFamily, 11, FontStyle.Italic);
        _emptyStateLabel.ForeColor = SystemColors.GrayText;
        gridHost.Controls.Add(_emptyStateLabel);
        gridHost.Controls.Add(_gamesGrid);
        gamePanelLayout.Controls.Add(gridHost, 0, 0);

        var actions = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill };
        var editButton = new Button { Text = "Edit Selected", Width = 120 };
        var removeButton = new Button { Text = "Remove Selected", Width = 130 };
        var openConfigButton = new Button { Text = "Open Config Folder", Width = 145 };
        editButton.Click += EditSelectedGame;
        removeButton.Click += RemoveSelectedGame;
        openConfigButton.Click += (_, _) => OpenFolder(_configService.ConfigDirectory, "Open Config Folder");
        actions.Controls.Add(editButton);
        actions.Controls.Add(removeButton);
        actions.Controls.Add(openConfigButton);
        gamePanelLayout.Controls.Add(actions, 0, 1);
        gamePanel.Controls.Add(gamePanelLayout);
        root.Controls.Add(gamePanel, 0, 2);

        var logsPanel = new GroupBox { Text = "Logs / Status", Dock = DockStyle.Fill };
        var logsLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        logsLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        logsLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        _logsTextBox.Dock = DockStyle.Fill;
        _logsTextBox.Multiline = true;
        _logsTextBox.ReadOnly = true;
        _logsTextBox.ScrollBars = ScrollBars.Both;
        _logsTextBox.WordWrap = false;
        _logsTextBox.Font = new Font(FontFamily.GenericMonospace, 9);
        logsLayout.Controls.Add(_logsTextBox, 0, 0);

        var logButtons = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill };
        var refreshLogsButton = new Button { Text = "Refresh Logs", Width = 110 };
        var openLogsButton = new Button { Text = "Open Logs Folder", Width = 135 };
        var openConfigFromLogsButton = new Button { Text = "Open Config Folder", Width = 145 };
        refreshLogsButton.Click += (_, _) => RefreshLogs();
        openLogsButton.Click += (_, _) => OpenFolder(_loggingService.LogDirectory, "Open Logs Folder");
        openConfigFromLogsButton.Click += (_, _) => OpenFolder(_configService.ConfigDirectory, "Open Config Folder");
        logButtons.Controls.Add(refreshLogsButton);
        logButtons.Controls.Add(openLogsButton);
        logButtons.Controls.Add(openConfigFromLogsButton);
        logsLayout.Controls.Add(logButtons, 0, 1);
        logsPanel.Controls.Add(logsLayout);
        root.Controls.Add(logsPanel, 0, 3);

        Controls.Add(root);
    }

    private void ConfigureGamesGrid()
    {
        _gamesGrid.Dock = DockStyle.Fill;
        _gamesGrid.AllowUserToAddRows = false;
        _gamesGrid.AllowUserToDeleteRows = false;
        _gamesGrid.ReadOnly = true;
        _gamesGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _gamesGrid.MultiSelect = false;
        _gamesGrid.AutoGenerateColumns = false;
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Name",
            DataPropertyName = nameof(GameConfig.Name),
            Width = 140
        });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Monitor",
            DataPropertyName = nameof(GameConfig.MonitorStatus),
            Width = 145
        });
        _gamesGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "Auto",
            DataPropertyName = nameof(GameConfig.AutoBackup),
            Width = 55
        });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Interval",
            DataPropertyName = nameof(GameConfig.AutoBackupIntervalDisplay),
            Width = 70
        });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Versions",
            DataPropertyName = nameof(GameConfig.MaxVersionBackups),
            Width = 70
        });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Backing Up",
            DataPropertyName = nameof(GameConfig.IsBackupRunning),
            Width = 85
        });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Last Auto Backup",
            DataPropertyName = nameof(GameConfig.LastAutoBackupTime),
            Width = 155
        });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Last Backup",
            DataPropertyName = nameof(GameConfig.LastBackupTime),
            Width = 155
        });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Remote",
            DataPropertyName = nameof(GameConfig.RcloneRemote),
            Width = 90
        });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Cloud Folder",
            DataPropertyName = nameof(GameConfig.CloudPath),
            Width = 170
        });
        _gamesGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = BackupColumnName,
            HeaderText = "Backup",
            Text = "Backup Now",
            UseColumnTextForButtonValue = true,
            Width = 105
        });
        _gamesGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = RestoreColumnName,
            HeaderText = "Restore",
            Text = "Restore",
            UseColumnTextForButtonValue = true,
            Width = 90
        });
        _gamesGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = HistoryColumnName,
            HeaderText = "History",
            Text = "History",
            UseColumnTextForButtonValue = true,
            Width = 80
        });
        _gamesGrid.Columns.Add(new DataGridViewButtonColumn
        {
            Name = OpenSaveFolderColumnName,
            HeaderText = "Folder",
            Text = "Open Save",
            UseColumnTextForButtonValue = true,
            Width = 105
        });
        _gamesGrid.CellContentClick += GamesGridCellContentClick;
        _gamesGrid.DoubleClick += EditSelectedGame;
    }

    private async Task RefreshRcloneStatusAsync()
    {
        _rcloneStatusLabel.Text = "Checking rclone availability...";

        var version = await _rcloneService.GetRcloneVersion(_appCts.Token);
        if (string.IsNullOrWhiteSpace(version))
        {
            _rcloneRemotes = [];
            _rcloneStatusLabel.Text =
                "Missing: rclone was not found. Bundle rclone with the app or install it in PATH, then refresh." +
                $"{Environment.NewLine}Config: {_rcloneService.RcloneConfigPath}";
            RefreshLogs();
            return;
        }

        _rcloneRemotes = await _rcloneService.ListRemotes(_appCts.Token);
        var remoteSummary = _rcloneRemotes.Count == 0
            ? "No configured remotes found. Click Configure Rclone to create one, for example `gdrive`."
            : $"{_rcloneRemotes.Count} remote(s): {string.Join(", ", _rcloneRemotes)}";
        var rcloneSource = _rcloneService.IsUsingBundledRclone
            ? "bundled"
            : "PATH fallback";
        _rcloneStatusLabel.Text =
            $"Installed ({rcloneSource}): {version}{Environment.NewLine}" +
            $"{remoteSummary}{Environment.NewLine}" +
            $"Config: {_rcloneService.RcloneConfigPath}";
        RefreshLogs();
    }

    private async Task CheckStartupRestorePromptsAsync()
    {
        if (_appConfig.Games.Count == 0)
        {
            return;
        }

        var rcloneVersion = await _rcloneService.GetRcloneVersion(_appCts.Token);
        if (string.IsNullOrWhiteSpace(rcloneVersion))
        {
            _loggingService.Info("Startup restore checks skipped because rclone is missing. Bundle rclone or install it in PATH.");
            RefreshLogs();
            return;
        }

        foreach (var game in _appConfig.Games.Where(game => game.StartupRestorePrompt))
        {
            _appCts.Token.ThrowIfCancellationRequested();
            if (_startupRestoreCheckedGameIds.Contains(game.Id))
            {
                continue;
            }

            _startupRestoreCheckedGameIds.Add(game.Id);

            if (string.IsNullOrWhiteSpace(game.RcloneRemote) || string.IsNullOrWhiteSpace(game.CloudPath))
            {
                _loggingService.Info($"Startup restore check skipped because remote/cloud path is missing: {game.Name}");
                continue;
            }

            var metadata = await _backupService.ReadCloudMetadataAsync(game, _appCts.Token);
            if (metadata is null)
            {
                _loggingService.Info($"Startup restore check found no usable cloud metadata: {game.Name}");
                continue;
            }

            var localSaveDate = _backupService.GetLocalSaveLastModified(game);
            var shouldPrompt = localSaveDate is null || metadata.LastBackupTime.ToUniversalTime() > localSaveDate.Value.ToUniversalTime();
            if (!shouldPrompt)
            {
                _loggingService.Info($"Startup restore check skipped prompt because local save is newer or equal: {game.Name}");
                continue;
            }

            await ShowStartupRestorePromptAsync(game, metadata, localSaveDate);
        }

        RefreshLogs();
    }

    private async Task ShowStartupRestorePromptAsync(GameConfig game, BackupMetadata metadata, DateTimeOffset? localSaveDate)
    {
        if (_startupRestorePromptedGameIds.Contains(game.Id))
        {
            return;
        }

        _startupRestorePromptedGameIds.Add(game.Id);

        var closedCheck = _backupService.VerifyGameIsClosedForRestore(game);
        if (!closedCheck.Succeeded)
        {
            MessageBox.Show(
                this,
                closedCheck.Message,
                "Startup Restore",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            RefreshLogs();
            return;
        }

        using var dialog = new RestorePromptDialog(game, metadata, localSaveDate);
        _ = dialog.ShowDialog(this);

        if (dialog.Choice == RestorePromptChoice.RestoreFromCloud)
        {
            UseWaitCursor = true;
            _gamesGrid.Enabled = false;
            try
            {
                var result = await _backupService.RestoreFromCloudAsync(game, _appCts.Token);
                MessageBox.Show(
                    this,
                    result.Message,
                    "Startup Restore",
                    MessageBoxButtons.OK,
                    result.Succeeded ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            finally
            {
                _gamesGrid.Enabled = true;
                UseWaitCursor = false;
                RefreshLogs();
            }
        }
        else if (dialog.Choice == RestorePromptChoice.KeepLocalSave)
        {
            _loggingService.Info($"Startup restore prompt dismissed: keep local save for {game.Name}");
        }
        else
        {
            _loggingService.Info($"Startup restore prompt dismissed: ask later for {game.Name}");
        }
    }

    private void AddGame(object? sender, EventArgs e)
    {
        using var form = new AddEditGameForm(_rcloneService, _rcloneRemotes);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _appConfig.Games.Add(form.Game);
            SaveConfigWithDialog();
            _gameMonitorService.UpdateGames(_appConfig.Games);
            _loggingService.Info($"Game added: {form.Game.Name}");
            RefreshGameList();
            RefreshLogs();
        }
    }

    private void EditSelectedGame(object? sender, EventArgs e)
    {
        var selected = GetSelectedGame();
        if (selected is null)
        {
            return;
        }

        using var form = new AddEditGameForm(_rcloneService, _rcloneRemotes, selected);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            var index = _appConfig.Games.FindIndex(game => game.Id == selected.Id);
            if (index >= 0)
            {
                _appConfig.Games[index] = form.Game;
                SaveConfigWithDialog();
                _gameMonitorService.UpdateGames(_appConfig.Games);
                _loggingService.Info($"Game edited: {form.Game.Name}");
                RefreshGameList();
                RefreshLogs();
            }
        }
    }

    private void RemoveSelectedGame(object? sender, EventArgs e)
    {
        var selected = GetSelectedGame();
        if (selected is null)
        {
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Remove '{selected.Name}' from the game list? This does not delete local saves or cloud backups.",
            "Remove Game",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _appConfig.Games.RemoveAll(game => game.Id == selected.Id);
        SaveConfigWithDialog();
        _gameMonitorService.UpdateGames(_appConfig.Games);
        _loggingService.Info($"Game removed: {selected.Name}");
        RefreshGameList();
        RefreshLogs();
    }

    private async void GamesGridCellContentClick(object? sender, DataGridViewCellEventArgs e)
    {
        if (e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (_gamesGrid.Rows[e.RowIndex].DataBoundItem is not GameConfig game)
        {
            return;
        }

        var columnName = _gamesGrid.Columns[e.ColumnIndex].Name;
        await SafeUiAsync(columnName, async () =>
        {
            if (columnName == BackupColumnName)
            {
                await BackupSelectedGameAsync(game);
            }
            else if (columnName == RestoreColumnName)
            {
                await RestoreSelectedGameAsync(game);
            }
            else if (columnName == HistoryColumnName)
            {
                ShowBackupHistory(game);
            }
            else if (columnName == OpenSaveFolderColumnName)
            {
                OpenSaveFolder(game);
            }
        });
    }

    private async Task BackupSelectedGameAsync(GameConfig game)
    {
        UseWaitCursor = true;
        _gamesGrid.Enabled = false;
        try
        {
            var result = await _backupService.BackupNowAsync(game, "manual", _appCts.Token);
            if (result.Succeeded)
            {
                SaveConfigWithDialog();
            }

            MessageBox.Show(
                this,
                result.Message,
                "Backup Now",
                MessageBoxButtons.OK,
                result.Succeeded ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        finally
        {
            _gamesGrid.Enabled = true;
            UseWaitCursor = false;
            RefreshGameList();
            RefreshLogs();
        }
    }

    private async Task RestoreSelectedGameAsync(GameConfig game)
    {
        var closedCheck = _backupService.VerifyGameIsClosedForRestore(game);
        if (!closedCheck.Succeeded)
        {
            MessageBox.Show(
                this,
                closedCheck.Message,
                "Restore from Cloud",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
            RefreshLogs();
            return;
        }

        UseWaitCursor = true;
        BackupMetadata? metadata;
        try
        {
            metadata = await _backupService.ReadCloudMetadataAsync(game, _appCts.Token);
        }
        finally
        {
            UseWaitCursor = false;
        }

        var metadataSummary = metadata is null
            ? "Cloud metadata could not be read. You can still attempt restore if the latest backup exists. " +
                "This is the part where we squint at the abyss."
            : $"Cloud backup date: {metadata.LastBackupTime.LocalDateTime}\n" +
                $"Source device: {metadata.SourceDevice}\n" +
                $"Backup type: {metadata.BackupType}";

        var confirm = MessageBox.Show(
            this,
            $"Restore '{game.Name}' from cloud latest?\n\n{metadataSummary}\n\n" +
                "Before restoring, the app will create a local safety backup of the current save folder. " +
                "Restore may overwrite local save files.",
            "Confirm Restore from Cloud",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        UseWaitCursor = true;
        _gamesGrid.Enabled = false;
        try
        {
            var result = await _backupService.RestoreFromCloudAsync(game, _appCts.Token);
            MessageBox.Show(
                this,
                result.Message,
                "Restore from Cloud",
                MessageBoxButtons.OK,
                result.Succeeded ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        finally
        {
            _gamesGrid.Enabled = true;
            UseWaitCursor = false;
            RefreshLogs();
        }
    }

    private void ShowBackupHistory(GameConfig game)
    {
        using var dialog = new BackupHistoryDialog(_backupService, game);
        dialog.ShowDialog(this);
        RefreshLogs();
    }

    private void OpenSaveFolder(GameConfig game)
    {
        if (string.IsNullOrWhiteSpace(game.SavePath) || !Directory.Exists(game.SavePath))
        {
            MessageBox.Show(this, "Save folder does not exist.", "Open Save Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        OpenFolder(game.SavePath, "Open Save Folder");
    }

    private void ShowRcloneHelp()
    {
        var message =
            "rclone setup quick path:\n\n" +
            "1. Use the bundled rclone that ships with the app, or install rclone separately as a fallback.\n" +
            "2. Click Configure Rclone and create a remote, for example gdrive.\n" +
            "3. Close the console window when configuration is finished.\n" +
            "4. Click Refresh Rclone, then use only the remote name in this app, like gdrive.\n\n" +
            $"Config file: {_rcloneService.RcloneConfigPath}\n\n" +
            "The README has the longer setup notes and examples.";

        var result = MessageBox.Show(
            this,
            message + "\n\nOpen rclone downloads page?",
            "Rclone Setup Help",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Information);
        if (result == DialogResult.Yes)
        {
            OpenUrl("https://rclone.org/downloads/");
        }
    }

    private void ConfigureRclone()
    {
        if (!_rcloneService.OpenRcloneConfig())
        {
            MessageBox.Show(
                this,
                "Could not start rclone config. Make sure bundled rclone exists or rclone is installed in PATH.",
                "Configure Rclone",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            RefreshLogs();
            return;
        }

        MessageBox.Show(
            this,
            "A console window opened for rclone config. Create or edit your remote there, close the console when done, then click Refresh Rclone.",
            "Configure Rclone",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
        RefreshLogs();
    }

    private void GameMonitorServiceStateChanged(object? sender, GameMonitorStateChangedEventArgs e)
    {
        if (IsDisposed || !IsHandleCreated)
        {
            return;
        }

        if (InvokeRequired)
        {
            try
            {
                BeginInvoke(new Action(() => GameMonitorServiceStateChanged(sender, e)));
            }
            catch (ObjectDisposedException)
            {
                // App is closing. Let the gremlin sleep.
            }
            return;
        }

        SaveConfigWithDialog(showSuccess: false);
        RefreshGameList();
        RefreshLogs();
    }

    private GameConfig? GetSelectedGame()
    {
        if (_gamesGrid.CurrentRow?.DataBoundItem is GameConfig game)
        {
            return game;
        }

        MessageBox.Show(this, "Select a game first.", "Game Selection", MessageBoxButtons.OK, MessageBoxIcon.Information);
        return null;
    }

    private void RefreshGameList()
    {
        var hasGames = _appConfig.Games.Count > 0;
        _gamesGrid.Visible = hasGames;
        _emptyStateLabel.Visible = !hasGames;
        _gamesGrid.DataSource = null;
        _gamesGrid.DataSource = _appConfig.Games.OrderBy(game => game.Name).ToList();
    }

    private void RefreshLogs()
    {
        var lines = _loggingService.GetRecentLines(300);
        _logsTextBox.Text = lines.Count == 0
            ? "No logs yet. Suspiciously peaceful."
            : string.Join(Environment.NewLine, lines);
        _logsTextBox.SelectionStart = _logsTextBox.TextLength;
        _logsTextBox.ScrollToCaret();
    }

    private void SaveConfigWithDialog(bool showSuccess = false)
    {
        try
        {
            _configService.Save(_appConfig);
            if (showSuccess)
            {
                MessageBox.Show(this, "Config saved.", "Config", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
        catch (Exception ex)
        {
            ShowError("Config", "Could not save config. Your current UI changes may not survive app restart.", ex);
        }
    }

    private async Task SafeUiAsync(string title, Func<Task> action)
    {
        try
        {
            await action();
        }
        catch (OperationCanceledException)
        {
            _loggingService.Info($"Canceled UI operation: {title}");
            RefreshLogs();
        }
        catch (Exception ex)
        {
            _loggingService.Error($"UI operation failed: {title}", ex);
            ShowError(title, "That operation failed. Details were written to the log.", ex);
            RefreshLogs();
        }
    }

    private void OpenFolder(string folderPath, string title)
    {
        try
        {
            Directory.CreateDirectory(folderPath);
            Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to open folder: {folderPath}", ex);
            ShowError(title, "Could not open folder.", ex);
            RefreshLogs();
        }
    }

    private void OpenUrl(string url)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to open URL: {url}", ex);
            ShowError("Open URL", "Could not open the browser.", ex);
            RefreshLogs();
        }
    }

    private void ShowError(string title, string message, Exception ex)
    {
        MessageBox.Show(this, $"{message}\n\nDetails: {ex.Message}", title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }
}
