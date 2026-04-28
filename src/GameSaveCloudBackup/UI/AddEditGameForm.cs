using GameSaveCloudBackup.Models;
using GameSaveCloudBackup.Services;

namespace GameSaveCloudBackup.UI;

public sealed class AddEditGameForm : Form
{
    private readonly RcloneService? _rcloneService;
    private readonly TextBox _nameTextBox = new();
    private readonly TextBox _exePathTextBox = new();
    private readonly TextBox _savePathTextBox = new();
    private readonly ComboBox _rcloneRemoteComboBox = new() { DropDownStyle = ComboBoxStyle.DropDown };
    private readonly TextBox _cloudPathTextBox = new();
    private readonly CheckBox _autoBackupCheckBox = new() { Checked = true };
    private readonly NumericUpDown _backupIntervalInput = new() { Minimum = 1, Maximum = 1440, Value = 10 };
    private readonly CheckBox _backupOnCloseCheckBox = new() { Checked = true };
    private readonly CheckBox _startupRestorePromptCheckBox = new() { Checked = true };
    private readonly NumericUpDown _maxVersionsInput = new() { Minimum = 0, Maximum = 365, Value = 10 };

    public GameConfig Game { get; private set; }

    public AddEditGameForm(RcloneService? rcloneService = null, IEnumerable<string>? remotes = null, GameConfig? game = null)
    {
        _rcloneService = rcloneService;
        Game = game is null ? new GameConfig() : Clone(game);
        Text = game is null ? "Add Game" : "Edit Game";
        StartPosition = FormStartPosition.CenterParent;
        Width = 800;
        Height = 530;
        MinimumSize = new Size(720, 500);
        BuildLayout();
        LoadRemotes(remotes ?? []);
        LoadGame();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(16),
            ColumnCount = 3,
            RowCount = 12,
            AutoSize = true
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 190));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 125));

        AddTextRow(root, 0, "Game Name", _nameTextBox);
        AddBrowseRow(root, 1, "Game EXE/Launcher", _exePathTextBox, BrowseExe);
        AddBrowseRow(root, 2, "Save Folder", _savePathTextBox, BrowseSaveFolder);
        AddRemoteRow(root, 3);
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

        root.Controls.Add(new Label { Text = "Startup Restore Prompt", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 8);
        root.Controls.Add(_startupRestorePromptCheckBox, 1, 8);
        root.SetColumnSpan(_startupRestorePromptCheckBox, 2);

        root.Controls.Add(new Label { Text = "Keep Versioned Backups", AutoSize = true, Anchor = AnchorStyles.Left }, 0, 9);
        root.Controls.Add(_maxVersionsInput, 1, 9);
        root.SetColumnSpan(_maxVersionsInput, 2);

        var helpText = new Label
        {
            Text = "Tip: Remote should be just the rclone name, like 'gdrive'. Cloud folder should be a folder path, like 'GameSaveBackups/Stardew Valley'. Set versions to 0 to keep all versioned backups.",
            Dock = DockStyle.Fill,
            AutoSize = false,
            ForeColor = SystemColors.GrayText
        };
        root.Controls.Add(helpText, 0, 10);
        root.SetColumnSpan(helpText, 3);

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var saveButton = new Button { Text = "Save", DialogResult = DialogResult.OK, Width = 90 };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, Width = 90 };
        saveButton.Click += (_, _) => ValidateAndSave();
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(cancelButton);
        root.Controls.Add(buttons, 0, 11);
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

    private void AddRemoteRow(TableLayoutPanel root, int row)
    {
        root.Controls.Add(new Label { Text = "Rclone Remote", AutoSize = true, Anchor = AnchorStyles.Left }, 0, row);
        _rcloneRemoteComboBox.Dock = DockStyle.Fill;
        root.Controls.Add(_rcloneRemoteComboBox, 1, row);
        var testRemoteButton = new Button { Text = "Test Remote", Dock = DockStyle.Fill };
        testRemoteButton.Click += TestRemote;
        root.Controls.Add(testRemoteButton, 2, row);
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

    private async void TestRemote(object? sender, EventArgs e)
    {
        var remoteName = _rcloneRemoteComboBox.Text.Trim().TrimEnd(':');
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            MessageBox.Show(this, "Enter or select an rclone remote first.", "Rclone Remote", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        if (remoteName.Contains(':'))
        {
            MessageBox.Show(this, "Use only the remote name here, for example 'gdrive'. Put folders in Cloud Backup Folder.", "Rclone Remote", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (_rcloneService is null)
        {
            MessageBox.Show(this, "Rclone service is unavailable.", "Rclone Remote", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        UseWaitCursor = true;
        try
        {
            var ok = await _rcloneService.TestRemote(remoteName);
            var message = ok
                ? $"Remote '{remoteName}' is reachable. Tiny miracle, accept it."
                : $"Remote '{remoteName}' could not be reached. Check rclone config, remote name, and cloud authentication.";
            MessageBox.Show(this, message, "Rclone Remote Test", MessageBoxButtons.OK, ok ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Remote test failed: {ex.Message}", "Rclone Remote Test", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void LoadRemotes(IEnumerable<string> remotes)
    {
        _rcloneRemoteComboBox.Items.Clear();
        foreach (var remote in remotes.Where(remote => !string.IsNullOrWhiteSpace(remote)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            _rcloneRemoteComboBox.Items.Add(remote.TrimEnd(':'));
        }
    }

    private void LoadGame()
    {
        _nameTextBox.Text = Game.Name;
        _exePathTextBox.Text = Game.ExePath;
        _savePathTextBox.Text = Game.SavePath;
        _rcloneRemoteComboBox.Text = Game.RcloneRemote;
        _cloudPathTextBox.Text = string.IsNullOrWhiteSpace(Game.CloudPath) ? "GameSaveBackups/" : Game.CloudPath;
        _autoBackupCheckBox.Checked = Game.AutoBackup;
        _backupIntervalInput.Value = Math.Clamp(Game.BackupIntervalMinutes, 1, 1440);
        _backupOnCloseCheckBox.Checked = Game.BackupOnClose;
        _startupRestorePromptCheckBox.Checked = Game.StartupRestorePrompt;
        _maxVersionsInput.Value = Math.Clamp(Game.MaxVersionBackups, 0, 365);
    }

    private bool ValidateAndSave()
    {
        var errors = new List<string>();
        var name = _nameTextBox.Text.Trim();
        var exePath = _exePathTextBox.Text.Trim();
        var savePath = _savePathTextBox.Text.Trim();
        var remote = _rcloneRemoteComboBox.Text.Trim().TrimEnd(':');
        var cloudPath = _cloudPathTextBox.Text.Trim().Trim('/', '\\');

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add("Game Name is required.");
        }

        if (string.IsNullOrWhiteSpace(exePath))
        {
            errors.Add("Game EXE/Launcher is required for monitoring.");
        }
        else if (!File.Exists(exePath))
        {
            errors.Add("Game EXE/Launcher does not exist.");
        }

        if (string.IsNullOrWhiteSpace(savePath))
        {
            errors.Add("Save Folder is required.");
        }
        else if (!Directory.Exists(savePath))
        {
            errors.Add("Save Folder does not exist.");
        }

        if (string.IsNullOrWhiteSpace(remote))
        {
            errors.Add("Rclone Remote is required.");
        }
        else if (remote.Contains(':') || remote.Contains('/') || remote.Contains('\\'))
        {
            errors.Add("Rclone Remote should be only a remote name, like 'gdrive'.");
        }

        if (string.IsNullOrWhiteSpace(cloudPath))
        {
            errors.Add("Cloud Backup Folder is required.");
        }
        else if (cloudPath.Contains(':'))
        {
            errors.Add("Cloud Backup Folder should not include the rclone remote name or colon.");
        }
        else if (!IsSafeCloudPath(cloudPath))
        {
            errors.Add("Cloud Backup Folder must be a relative folder path and cannot contain '.' or '..' path segments.");
        }

        if (errors.Count > 0)
        {
            MessageBox.Show(this, string.Join(Environment.NewLine, errors), "Please fix these settings", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return false;
        }

        Game.Name = name;
        Game.ExePath = exePath;
        Game.SavePath = savePath;
        Game.RcloneRemote = remote;
        Game.CloudPath = cloudPath;
        Game.AutoBackup = _autoBackupCheckBox.Checked;
        Game.BackupIntervalMinutes = (int)_backupIntervalInput.Value;
        Game.BackupOnClose = _backupOnCloseCheckBox.Checked;
        Game.StartupRestorePrompt = _startupRestorePromptCheckBox.Checked;
        Game.MaxVersionBackups = (int)_maxVersionsInput.Value;
        return true;
    }

    private static bool IsSafeCloudPath(string cloudPath)
    {
        var normalized = cloudPath.Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .All(segment => segment != "." && segment != "..");
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
