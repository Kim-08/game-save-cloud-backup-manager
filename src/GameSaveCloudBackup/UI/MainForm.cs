using GameSaveCloudBackup.Models;
using GameSaveCloudBackup.Services;

namespace GameSaveCloudBackup.UI;

public sealed class MainForm : Form
{
    private readonly ConfigService _configService;
    private readonly LoggingService _loggingService;
    private readonly RcloneService _rcloneService;
    private readonly AppConfig _appConfig;
    private readonly DataGridView _gamesGrid = new();
    private readonly ListBox _logsList = new();
    private readonly Label _rcloneStatusLabel = new();
    private IReadOnlyList<string> _rcloneRemotes = [];

    public MainForm(ConfigService configService, LoggingService loggingService, RcloneService rcloneService, AppConfig appConfig)
    {
        _configService = configService;
        _loggingService = loggingService;
        _rcloneService = rcloneService;
        _appConfig = appConfig;

        Text = "Game Save Cloud Backup Manager";
        StartPosition = FormStartPosition.CenterScreen;
        Width = 1100;
        Height = 720;
        MinimumSize = new Size(900, 600);

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
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Name", DataPropertyName = nameof(GameConfig.Name), Width = 180 });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "EXE / Launcher", DataPropertyName = nameof(GameConfig.ExePath), Width = 260 });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Save Folder", DataPropertyName = nameof(GameConfig.SavePath), Width = 260 });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Remote", DataPropertyName = nameof(GameConfig.RcloneRemote), Width = 120 });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Cloud Folder", DataPropertyName = nameof(GameConfig.CloudPath), Width = 180 });
        _gamesGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "Auto", DataPropertyName = nameof(GameConfig.AutoBackup), Width = 60 });
        _gamesGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Interval", DataPropertyName = nameof(GameConfig.BackupIntervalMinutes), Width = 70 });
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
