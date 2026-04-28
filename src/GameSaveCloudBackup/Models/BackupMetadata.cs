namespace GameSaveCloudBackup.Models;

public sealed class BackupMetadata
{
    public string GameName { get; set; } = string.Empty;
    public DateTimeOffset LastBackupTime { get; set; }
    public string SourceDevice { get; set; } = Environment.MachineName;
    public string BackupType { get; set; } = string.Empty;
    public string SavePath { get; set; } = string.Empty;
    public string AppVersion { get; set; } = "0.1.0";
}
