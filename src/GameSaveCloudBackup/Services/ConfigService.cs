using System.Text.Json;
using GameSaveCloudBackup.Models;

namespace GameSaveCloudBackup.Services;

public sealed class ConfigService
{
    private readonly LoggingService _loggingService;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public string ConfigDirectory { get; }
    public string ConfigFilePath { get; }

    public ConfigService(LoggingService loggingService)
    {
        _loggingService = loggingService;
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        ConfigDirectory = Path.Combine(appData, "GameSaveCloudBackup");
        ConfigFilePath = Path.Combine(ConfigDirectory, "config.json");
    }

    public AppConfig Load()
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);
            if (!File.Exists(ConfigFilePath))
            {
                var emptyConfig = new AppConfig();
                Save(emptyConfig);
                _loggingService.Info("Config loaded: created default config");
                return emptyConfig;
            }

            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();
            config.Games ??= [];
            _loggingService.Info("Config loaded");
            return config;
        }
        catch (Exception ex)
        {
            _loggingService.Error("Error loading config", ex);
            return new AppConfig();
        }
    }

    public void Save(AppConfig config)
    {
        try
        {
            Directory.CreateDirectory(ConfigDirectory);
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(ConfigFilePath, json);
            _loggingService.Info("Config saved");
        }
        catch (Exception ex)
        {
            _loggingService.Error("Error saving config", ex);
            throw;
        }
    }
}
