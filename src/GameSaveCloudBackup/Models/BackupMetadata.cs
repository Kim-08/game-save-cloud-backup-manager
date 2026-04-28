using System.Text.Json.Serialization;

namespace GameSaveCloudBackup.Models;

public sealed class BackupMetadata
{
    [JsonPropertyName("gameName")]
    public string GameName { get; set; } = string.Empty;

    [JsonPropertyName("lastBackupTime")]
    public DateTimeOffset LastBackupTime { get; set; }

    [JsonPropertyName("sourceDevice")]
    public string SourceDevice { get; set; } = Environment.MachineName;

    [JsonPropertyName("backupType")]
    public string BackupType { get; set; } = string.Empty;

    [JsonPropertyName("savePath")]
    public string SavePath { get; set; } = string.Empty;

    [JsonPropertyName("appVersion")]
    public string AppVersion { get; set; } = "0.1.0";
}
