using GameSaveCloudBackup.Models;

namespace GameSaveCloudBackup.UI;

public enum RestorePromptChoice
{
    RestoreFromCloud,
    KeepLocalSave,
    AskLater
}

public sealed class RestorePromptDialog : Form
{
    private RestorePromptChoice _choice = RestorePromptChoice.AskLater;

    public RestorePromptChoice Choice => _choice;

    public RestorePromptDialog(GameConfig game, BackupMetadata metadata, DateTimeOffset? localSaveDate)
    {
        Text = "Cloud Backup Found";
        StartPosition = FormStartPosition.CenterParent;
        Width = 560;
        Height = 330;
        MinimumSize = new Size(520, 300);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;

        BuildLayout(game, metadata, localSaveDate);
    }

    private void BuildLayout(GameConfig game, BackupMetadata metadata, DateTimeOffset? localSaveDate)
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            RowCount = 2,
            ColumnCount = 1
        };
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));

        var localDateText = localSaveDate.HasValue ? localSaveDate.Value.LocalDateTime.ToString("f") : "Local save folder missing or unavailable";
        var message =
            $"Cloud backup found for {game.Name}.\n\n" +
            $"Cloud backup date: {metadata.LastBackupTime.LocalDateTime:f}\n" +
            $"Created from: {metadata.SourceDevice}\n" +
            $"Local save date: {localDateText}\n\n" +
            "Do you want to restore this cloud save?\n\n" +
            "A local safety backup will be created before restore. If something goes sideways, at least we left breadcrumbs.";

        root.Controls.Add(new Label
        {
            Text = message,
            Dock = DockStyle.Fill,
            AutoSize = false,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var buttons = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft
        };

        var restoreButton = new Button { Text = "Restore from Cloud", Width = 135, DialogResult = DialogResult.OK };
        restoreButton.Click += (_, _) => _choice = RestorePromptChoice.RestoreFromCloud;

        var keepButton = new Button { Text = "Keep Local Save", Width = 120, DialogResult = DialogResult.OK };
        keepButton.Click += (_, _) => _choice = RestorePromptChoice.KeepLocalSave;

        var askLaterButton = new Button { Text = "Ask Later", Width = 95, DialogResult = DialogResult.Cancel };
        askLaterButton.Click += (_, _) => _choice = RestorePromptChoice.AskLater;

        buttons.Controls.Add(restoreButton);
        buttons.Controls.Add(keepButton);
        buttons.Controls.Add(askLaterButton);
        root.Controls.Add(buttons, 0, 1);

        AcceptButton = restoreButton;
        CancelButton = askLaterButton;
        Controls.Add(root);
    }
}
