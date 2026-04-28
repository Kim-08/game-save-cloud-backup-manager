using GameSaveCloudBackup.Models;
using GameSaveCloudBackup.Services;

namespace GameSaveCloudBackup.UI;

public sealed class BackupHistoryDialog : Form
{
    private readonly BackupService _backupService;
    private readonly GameConfig _game;
    private readonly DataGridView _historyGrid = new();
    private readonly Label _statusLabel = new();

    public BackupHistoryDialog(BackupService backupService, GameConfig game)
    {
        _backupService = backupService;
        _game = game;

        Text = $"Backup History - {game.Name}";
        StartPosition = FormStartPosition.CenterParent;
        Width = 780;
        Height = 420;
        MinimumSize = new Size(640, 320);

        BuildLayout();
    }

    protected override async void OnShown(EventArgs e)
    {
        base.OnShown(e);
        await LoadHistoryAsync();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            RowCount = 3,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

        _statusLabel.Text = "Loading cloud versions...";
        _statusLabel.Dock = DockStyle.Fill;
        _statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        root.Controls.Add(_statusLabel, 0, 0);

        _historyGrid.Dock = DockStyle.Fill;
        _historyGrid.AllowUserToAddRows = false;
        _historyGrid.AllowUserToDeleteRows = false;
        _historyGrid.ReadOnly = true;
        _historyGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _historyGrid.AutoGenerateColumns = false;
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Version", DataPropertyName = nameof(BackupHistoryRow.Name), Width = 160 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Created", DataPropertyName = nameof(BackupHistoryRow.CreatedAt), Width = 200 });
        _historyGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Remote Path", DataPropertyName = nameof(BackupHistoryRow.RemotePath), AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        root.Controls.Add(_historyGrid, 0, 1);

        var buttons = new FlowLayoutPanel { FlowDirection = FlowDirection.RightToLeft, Dock = DockStyle.Fill };
        var closeButton = new Button { Text = "Close", DialogResult = DialogResult.OK, Width = 90 };
        var refreshButton = new Button { Text = "Refresh", Width = 90 };
        refreshButton.Click += async (_, _) => await LoadHistoryAsync();
        buttons.Controls.Add(closeButton);
        buttons.Controls.Add(refreshButton);
        root.Controls.Add(buttons, 0, 2);

        AcceptButton = closeButton;
        CancelButton = closeButton;
        Controls.Add(root);
    }

    private async Task LoadHistoryAsync()
    {
        UseWaitCursor = true;
        _statusLabel.Text = "Loading cloud versions...";
        try
        {
            var history = await _backupService.GetBackupHistoryAsync(_game);
            var rows = history
                .Select(entry => new BackupHistoryRow(
                    entry.Name,
                    entry.CreatedAt?.LocalDateTime.ToString("f") ?? "Unknown",
                    entry.RemotePath))
                .ToList();

            _historyGrid.DataSource = rows;
            _statusLabel.Text = rows.Count == 0
                ? "No versioned backups found in the cloud versions folder. The void is tidy today."
                : $"Found {rows.Count} versioned backup(s). Retention keeps the latest {_game.MaxVersionBackups} unless set to 0.";
        }
        catch (Exception ex)
        {
            _statusLabel.Text = "Could not load backup history.";
            MessageBox.Show(this, $"Could not load backup history: {ex.Message}", "Backup History", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private sealed record BackupHistoryRow(string Name, string CreatedAt, string RemotePath);
}
