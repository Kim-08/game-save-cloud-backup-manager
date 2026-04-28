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
            using var stream = new FileStream(ConfigFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var config = JsonSerializer.Deserialize<AppConfig>(stream, _jsonOptions) ?? new AppConfig();
            config.Games ??= [];
            _loggingService.Info("Config loaded");
            return config;
        }
        catch (JsonException ex)
        {
            return RecoverFromCorruptConfig(ex);
        }
        catch (NotSupportedException ex)
        {
            return RecoverFromCorruptConfig(ex);
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
            var tempPath = Path.Combine(ConfigDirectory, $"config.{Guid.NewGuid():N}.tmp");
            var json = JsonSerializer.Serialize(config, _jsonOptions);
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, ConfigFilePath, overwrite: true);
            _loggingService.Info("Config saved");
        }
        catch (Exception ex)
        {
            _loggingService.Error("Error saving config", ex);
            throw;
        }
    }

    private AppConfig RecoverFromCorruptConfig(Exception exception)
    {
        var corruptPath = RenameCorruptConfig();
        var recoveryMessage = corruptPath is null
            ? "Config file was invalid, but it could not be renamed. A new clean config.json was created."
            : $"Config file was invalid and has been renamed to {corruptPath}. A new clean config.json was created.";

        _loggingService.Error(recoveryMessage, exception);

        var emptyConfig = new AppConfig();
        Save(emptyConfig);
        return emptyConfig;
    }

    private string? RenameCorruptConfig()
    {
        try
        {
            var corruptPath = CreateCorruptConfigPath();
            File.Move(ConfigFilePath, corruptPath, overwrite: false);
            return corruptPath;
        }
        catch (Exception ex)
        {
            _loggingService.Error("Failed to rename corrupt config file", ex);
            return null;
        }
    }

    private string CreateCorruptConfigPath()
    {
        var timestamp = DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss_fff");
        var corruptPath = Path.Combine(ConfigDirectory, $"config.corrupt.{timestamp}.json");

        return File.Exists(corruptPath)
            ? Path.Combine(ConfigDirectory, $"config.corrupt.{timestamp}.{Guid.NewGuid():N}.json")
            : corruptPath;
    }
}
