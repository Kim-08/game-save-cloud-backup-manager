using GameSaveCloudBackup.Models;

namespace GameSaveCloudBackup.UI;

public sealed class AddEditGameForm : Form
{
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _exePathTextBox = new();
    private readonly TextBox _savePathTextBox = new();
    private readonly TextBox _rcloneRemoteTextBox = new();
    private readonly TextBox _cloudPathTextBox = new();
    private readonly CheckBox _autoBackupCheckBox = new() { Checked = true };
    private readonly NumericUpDown _backupIntervalInput = new() { Minimum = 1, Maximum = 1440, Value = 10 };
    private readonly CheckBox _backupOnCloseCheckBox = new() { Checked = true };

    public GameConfig Game { get; private set; }

    public AddEditGameForm(GameConfig? game = null)
    {
        Game = game is null ? new GameConfig() : Clone(game);
        Text = game is null ? "Add Game" : "Edit Game";
        StartPosition = FormStartPosition.CenterParent;
        Width = 700;
        Height = 430;
        MinimumSize = new Size(650, 420);
        BuildLayout();
        LoadGame();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 3,
            RowCount = 9,
            AutoSize = true
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

        AddTextRow(root, 0, "Game Name", _nameTextBox);
        AddBrowseRow(root, 1, "Game EXE/Launcher", _exePathTextBox, BrowseExe);
        AddBrowseRow(root, 2, "Save Folder", _savePathTextBox, BrowseSaveFolder);
        AddTextRow(root, 3, "Rclone Remote", _rcloneRemoteTextBox);
        AddTextRow(root, 4, "Cloud Backup Folder", _cloudPathTextBox);

        root.Controls.Add(new Label { Text = "Auto Backup", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 5);
        root.Controls.Add(_autoBackupCheckBox, 1, 5);
        root.SetColumnSpan(_autoBackupCheckBox, 2);

        root.Controls.Add(new Label { Text = "Backup Interval (minutes)", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 6);
        root.Controls.Add(_backupIntervalInput, 1, 6);
        root.SetColumnSpan(_backupIntervalInput, 2);

        root.Controls.Add(new Label { Text = "Backup on Close", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 7);
        root.Controls.Add(_backupOnCloseCheckBox, 1, 7);
        root.SetColumnSpan(_backupOnCloseCheckBox, 2);

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        saveButton.Click += (_, e) =>
        {
            if (!ValidateAndSave())
            {
                e = EventArgs.Empty;
            }
        };
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        root.Controls.Add(buttons, 0, 8);
        root.SetColumnSpan(buttons, 3);

        AcceptButton = saveButton;
        CancelButton = cancelButton;
        Controls.Add(root);
    }

    private static void AddTextRow(TableLayoutPanel root, int row, string label, TextBox textBox)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        textBox.Dock = DockStyle.Fill;
        root.Controls.Add(textBox, 1, row);
        root.SetColumnSpan(textBox, 2);
    }

    private static void AddBrowseRow(TableLayoutPanel root, int row, string label, TextBox textBox, EventHandler browseHandler)
    {
        root.Controls.Add(new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        textBox.Dock = DockStyle.Fill;
        root.Controls.Add(textBox, 1, row);
        var browseButton = new Button { Text = "Browse", Dock = DockStyle.Fill };
        browseButton.Click += browseHandler;
        root.Controls.Add(browseButton, 2, row);
    }

    private void BrowseExe(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Title = "Select game EXE or launcher",
            Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*"
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _exePathTextBox.Text = dialog.FileName;
        }
    }

    private void BrowseSaveFolder(object? sender, EventArgs e)
    {
        using var dialog = new FolderBrowserDialog { Description = "Select game save folder" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _savePathTextBox.Text = dialog.SelectedPath;
        }
    }

    private void LoadGame()
    {
        _nameTextBox.Text = Game.Name;
        _exePathTextBox.Text = Game.ExePath;
        _savePathTextBox.Text = Game.SavePath;
        _rcloneRemoteTextBox.Text = Game.RcloneRemote;
        _cloudPathTextBox.Text = string.IsNullOrWhiteSpace(Game.CloudPath) ? "GameSaveBackups/" : Game.CloudPath;
        _autoBackupCheckBox.Checked = Game.AutoBackup;
        _backupIntervalInput.Value = Math.Clamp(Game.BackupIntervalMinutes, 1, 1440);
        _backupOnCloseCheckBox.Checked = Game.BackupOnClose;
    }

    private bool ValidateAndSave()
    {
        if (string.IsNullOrWhiteSpace(_nameTextBox.Text))
        {
            MessageBox.Show(this, "Game Name is required.", "Validation", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return false;
        }

        Game.Name = _nameTextBox.Text.Trim();
        Game.ExePath = _exePathTextBox.Text.Trim();
        Game.SavePath = _savePathTextBox.Text.Trim();
        Game.RcloneRemote = _rcloneRemoteTextBox.Text.Trim();
        Game.CloudPath = _cloudPathTextBox.Text.Trim();
        Game.AutoBackup = _autoBackupCheckBox.Checked;
        Game.BackupIntervalMinutes = (int)_backupIntervalInput.Value;
        Game.BackupOnClose = _backupOnCloseCheckBox.Checked;
        return true;
    }

    private static GameConfig Clone(GameConfig game) => new()
    {
        Id = game.Id,
        Name = game.Name,
        ExePath = game.ExePath,
        SavePath = game.SavePath,
        RcloneRemote = game.RcloneRemote,
        CloudPath = game.CloudPath,
        AutoBackup = game.AutoBackup,
        BackupIntervalMinutes = game.BackupIntervalMinutes,
        BackupOnClose = game.BackupOnClose,
        StartupRestorePrompt = game.StartupRestorePrompt,
        MaxVersionBackups = game.MaxVersionBackups,
        LastBackupTime = game.LastBackupTime
    };
}
