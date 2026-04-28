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
        Directory.CreateDirectory(ConfigDirectory);

        if (!File.Exists(ConfigFilePath))
        {
            var emptyConfig = new AppConfig();
            Save(emptyConfig);
            _loggingService.Info("Config loaded: created default config");
            return emptyConfig;
        }

        try
        {
            var json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions) ?? new AppConfig();
            config.Games ??= [];
            _loggingService.Info("Config loaded");
            return config;
        }
        catch (JsonException ex)
        {
            var backupPath = BackupBadConfig();
            _loggingService.Error($"Config was invalid JSON and has been backed up to {backupPath}. A fresh config was created.", ex);
            var emptyConfig = new AppConfig();
            Save(emptyConfig);
            return emptyConfig;
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
            var tempPath = ConfigFilePath + ".tmp";
            File.WriteAllText(tempPath, json);
            File.Copy(tempPath, ConfigFilePath, overwrite: true);
            File.Delete(tempPath);
            _loggingService.Info("Config saved");
        }
        catch (Exception ex)
        {
            _loggingService.Error("Error saving config", ex);
            throw;
        }
    }

    private string BackupBadConfig()
    {
        try
        {
            var backupPath = Path.Combine(ConfigDirectory, $"config.bad.{DateTimeOffset.Now:yyyyMMdd_HHmmss}.json");
            File.Copy(ConfigFilePath, backupPath, overwrite: false);
            return backupPath;
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to back up corrupt config", ex);
            return "backup failed";
        }
    }
}
