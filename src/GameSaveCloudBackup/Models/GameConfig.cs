using System.Text.Json.Serialization;

namespace GameSaveCloudBackup.Models;

public sealed class GameConfig
{
    [JsonPropertyName("id")]
    public Guid Id { get; set; } = Guid.NewGuid();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("exePath")]
    public string ExePath { get; set; } = string.Empty;

    [JsonPropertyName("savePath")]
    public string SavePath { get; set; } = string.Empty;

    [JsonPropertyName("rcloneRemote")]
    public string RcloneRemote { get; set; } = string.Empty;

    [JsonPropertyName("cloudPath")]
    public string CloudPath { get; set; } = string.Empty;

    [JsonPropertyName("autoBackup")]
    public bool AutoBackup { get; set; } = true;

    [JsonPropertyName("backupIntervalMinutes")]
    public int BackupIntervalMinutes { get; set; } = 10;

    [JsonPropertyName("backupOnClose")]
    public bool BackupOnClose { get; set; } = true;

    [JsonPropertyName("startupRestorePrompt")]
    public bool StartupRestorePrompt { get; set; } = true;

    [JsonPropertyName("maxVersionBackups")]
    public int MaxVersionBackups { get; set; } = 10;

    [JsonPropertyName("lastBackupTime")]
    public DateTimeOffset? LastBackupTime { get; set; }

    [JsonIgnore]
    public string MonitorStatus { get; set; } = "Not Running";

    [JsonIgnore]
    public DateTimeOffset? LastAutoBackupTime { get; set; }

    [JsonIgnore]
    public bool IsBackupRunning { get; set; }

    [JsonIgnore]
    public string AutoBackupIntervalDisplay { get; set; } = "10 min";
}
