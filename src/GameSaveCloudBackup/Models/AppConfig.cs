using System.Text.Json.Serialization;

namespace GameSaveCloudBackup.Models;

public sealed class AppConfig
{
    [JsonPropertyName("games")]
    public List<GameConfig> Games { get; set; } = [];
}
