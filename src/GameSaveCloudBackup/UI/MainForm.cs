using System.Diagnostics;
using GameSaveCloudBackup.Models;
using GameSaveCloudBackup.Services;

namespace GameSaveCloudBackup.UI;

public sealed class MainForm : Form
{
    private const string BackupColumnName = "BackupNow";
    private const string RestoreColumnName = "RestoreFromCloud";
    private const string OpenSaveFolderColumnName = "OpenSaveFolder";

    private readonly ConfigService _configService;
    private readonly LoggingService _loggingService;
    private readonly RcloneService _rcloneService;
    private readonly BackupService _backupService;
    private readonly AppConfig _appConfig;
    private readonly DataGridView _gamesGrid = new();
    private readonly ListBox _logsList = new();
    private readonly Label _rcloneStatusLabel = new();
    private IReadOnlyList<string> _rcloneRemotes = [];

    public MainForm(ConfigService configService, LoggingService loggingService, RcloneService rcloneService, BackupService backupService, AppConfig appConfig)
    {
        _configService = configService;
        _loggingService = loggingService;
        _rcloneService = rcloneService;
        _backupService = backupService;
        _appConfig = appConfig;

        Text = "Game Save Cloud Backup Manager";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1280;
        Height = 760;
        MinimumSize = new Size(1050, 650);

        BuildLayout();
        RefreshGameList();
        RefreshLogs();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await RefreshRcloneStatusAsync();
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
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 110));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 65));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 35));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
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
        var rcloneLayout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2 };
        rcloneLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        rcloneLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        _rcloneStatusLabel.Text = "Checking rclone availability...";
        _rcloneStatusLabel.Dock = DockStyle.Fill;
        _rcloneStatusLabel.TextAlign = ContentAlignment.MiddleLeft;
        _rcloneStatusLabel.Padding = new Padding(10, 0, 0, 0);
        var refreshRcloneButton = new Button { Text = "Refresh Rclone", Dock = DockStyle.Fill };
        refreshRcloneButton.Click += async (_, _) => await RefreshRcloneStatusAsync();
        rcloneLayout.Controls.Add(_rcloneStatusLabel, 0, 0);
        rcloneLayout.Controls.Add(refreshRcloneButton, 1, 0);
        rclonePanel.Controls.Add(rcloneLayout);
        root.Controls.Add(rclonePanel, 0, 1);

        var gamePanel = new GroupBox { Text = "Games", Dock = DockStyle.Fill };
        var gamePanelLayout = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
        gamePanelLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        gamePanelLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
        ConfigureGamesGrid();
        gamePanelLayout.Controls.Add(_gamesGrid, 0, 0);

        var actions = new FlowLayoutPanel { FlowDirection = FlowDirection.LeftToRight, Dock = DockStyle.Fill };
        var editButton = new Button { Text = "Edit Selected", Width = 120 };
        var removeButton = new Button { Text = "Remove Selected", Width = 130 };
        editButton.Click += EditSelectedGame;
        removeButton.Click += RemoveSelectedGame;
        actions.Controls.Add(editButton);
        actions.Controls.Add(removeButton);
        gamePanelLayout.Controls.Add(actions, 0, 1);
        gamePanel.Controls.Add(gamePanelLayout);
        root.Controls.Add(gamePanel, 0, 2);

        var logsPanel = new GroupBox { Text = "Recent Logs / Status", Dock = DockStyle.Fill };
        _logsList.Dock = DockStyle.Fill;
        logsPanel.Controls.Add(_logsList);
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
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = nameof(GameConfig.Name), Width = 160 });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Save Folder", DataPropertyName = nameof(GameConfig.SavePath), Width = 250 });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Remote", DataPropertyName = nameof(GameConfig.RcloneRemote), Width = 100 });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cloud Folder", DataPropertyName = nameof(GameConfig.CloudPath), Width = 210 });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Last Backup", DataPropertyName = nameof(GameConfig.LastBackupTime), Width = 160 });
        _gamesGrid.Columns.Add(new DataGridViewButtonColumn { Name = BackupColumnName, HeaderText = "Backup", Text = "Backup Now", UseColumnTextForButtonValue = true, Width = 105 });
        _gamesGrid.Columns.Add(new DataGridViewButtonColumn { Name = RestoreColumnName, HeaderText = "Restore", Text = "Restore from Cloud", UseColumnTextForButtonValue = true, Width = 135 });
        _gamesGrid.Columns.Add(new DataGridViewButtonColumn { Name = OpenSaveFolderColumnName, HeaderText = "Folder", Text = "Open Save Folder", UseColumnTextForButtonValue = true, Width = 130 });
        _gamesGrid.CellContentClick += GamesGridCellContentClick;
        _gamesGrid.DoubleClick += EditSelectedGame;
    }

    private async Task RefreshRcloneStatusAsync()
    {
        _rcloneStatusLabel.Text = "Checking rclone availability...";

        var version = await _rcloneService.GetRcloneVersion();
        if (string.IsNullOrWhiteSpace(version))
        {
            _rcloneRemotes = [];
            _rcloneStatusLabel.Text = "Missing: rclone is not installed or is not available in PATH. Install rclone, run `rclone config`, then refresh.";
            RefreshLogs();
            return;
        }

        _rcloneRemotes = await _rcloneService.ListRemotes();
        var remoteSummary = _rcloneRemotes.Count == 0
            ? "No configured remotes found. Run `rclone config` to create one, for example `gdrive`."
            : $"{_rcloneRemotes.Count} remote(s): {string.Join(", ", _rcloneRemotes)}";
        _rcloneStatusLabel.Text = $"Installed: {version}{Environment.NewLine}{remoteSummary}";
        RefreshLogs();
    }

    private void AddGame(object? sender, EventArgs e)
    {
        using var form = new AddEditGameForm(_rcloneService, _rcloneRemotes);
        if (form.ShowDialog(this) == DialogResult.OK)
        {
            _appConfig.Games.Add(form.Game);
            _configService.Save(_appConfig);
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
                _configService.Save(_appConfig);
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

        var result = MessageBox.Show(this, $"Remove '{selected.Name}' from the game list?", "Remove Game", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (result != DialogResult.Yes)
        {
            return;
        }

        _appConfig.Games.RemoveAll(game => game.Id == selected.Id);
        _configService.Save(_appConfig);
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
        if (columnName == BackupColumnName)
        {
            await BackupSelectedGameAsync(game);
        }
        else if (columnName == RestoreColumnName)
        {
            await RestoreSelectedGameAsync(game);
        }
        else if (columnName == OpenSaveFolderColumnName)
        {
            OpenSaveFolder(game);
        }
    }

    private async Task BackupSelectedGameAsync(GameConfig game)
    {
        UseWaitCursor = true;
        _gamesGrid.Enabled = false;
        try
        {
            var result = await _backupService.BackupNowAsync(game, "manual");
            if (result.Succeeded)
            {
                _configService.Save(_appConfig);
            }

            MessageBox.Show(this, result.Message, "Backup Now", MessageBoxButtons.OK, result.Succeeded ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
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
        UseWaitCursor = true;
        BackupMetadata? metadata;
        try
        {
            metadata = await _backupService.ReadCloudMetadataAsync(game);
        }
        finally
        {
            UseWaitCursor = false;
        }

        var metadataSummary = metadata is null
            ? "Cloud metadata could not be read. You can still attempt restore if the latest backup exists."
            : $"Cloud backup date: {metadata.LastBackupTime.LocalDateTime}\nSource device: {metadata.SourceDevice}\nBackup type: {metadata.BackupType}";

        var confirm = MessageBox.Show(
            this,
            $"Restore '{game.Name}' from cloud latest?\n\n{metadataSummary}\n\nBefore restoring, the app will create a local safety backup of the current save folder. Restore may overwrite local save files.",
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
            var result = await _backupService.RestoreFromCloudAsync(game);
            MessageBox.Show(this, result.Message, "Restore from Cloud", MessageBoxButtons.OK, result.Succeeded ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        finally
        {
            _gamesGrid.Enabled = true;
            UseWaitCursor = false;
            RefreshLogs();
        }
    }

    private void OpenSaveFolder(GameConfig game)
    {
        if (string.IsNullOrWhiteSpace(game.SavePath) || !Directory.Exists(game.SavePath))
        {
            MessageBox.Show(this, "Save folder does not exist.", "Open Save Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = game.SavePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to open save folder: {game.Name}", ex);
            MessageBox.Show(this, $"Could not open save folder: {ex.Message}", "Open Save Folder", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            RefreshLogs();
        }
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
        _gamesGrid.DataSource = null;
        _gamesGrid.DataSource = _appConfig.Games.OrderBy(game => game.Name).ToList();
    }

    private void RefreshLogs()
    {
        _logsList.Items.Clear();
        foreach (var line in _loggingService.GetRecentLines(100))
        {
            _logsList.Items.Add(line);
        }

        if (_logsList.Items.Count > 0)
        {
            _logsList.TopIndex = _logsList.Items.Count - 1;
        }
    }
}
