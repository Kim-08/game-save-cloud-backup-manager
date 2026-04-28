using System.Reflection;
using System.Text.Json;
using GameSaveCloudBackup.Models;

namespace GameSaveCloudBackup.Services;

public sealed class BackupService
{
    private readonly RcloneService _rcloneService;
    private readonly LoggingService _loggingService;
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public BackupService(RcloneService rcloneService, LoggingService loggingService)
    {
        _rcloneService = rcloneService;
        _loggingService = loggingService;
    }

    public async Task<BackupOperationResult> BackupNowAsync(GameConfig game, string backupType, CancellationToken cancellationToken = default)
    {
        try
        {
            _loggingService.Info($"Manual backup started: {game.Name}");

            var validation = await ValidateGameForRcloneOperationAsync(game, requireSaveFolder: true, cancellationToken);
            if (!validation.Succeeded)
            {
                return validation;
            }

            if (IsSaveFolderEmpty(game))
            {
                return BackupOperationResult.Fail("Save folder is empty. Backup was rejected to avoid uploading an empty save set.");
            }

            var latestRemotePath = _rcloneService.BuildRemotePath(game.RcloneRemote, game.CloudPath, "latest");
            var versionRemotePath = _rcloneService.BuildRemotePath(game.RcloneRemote, game.CloudPath, $"versions/{CreateTimestamp()}");

            var latestResult = await _rcloneService.RunRcloneCommandAsync(
                $"copy {QuoteArgument(game.SavePath)} {QuoteArgument(latestRemotePath)} --create-empty-src-dirs",
                cancellationToken);
            if (!latestResult.Succeeded)
            {
                return BackupOperationResult.Fail($"Failed to upload latest backup. {CleanError(latestResult.StandardError)}");
            }

            var versionResult = await _rcloneService.RunRcloneCommandAsync(
                $"copy {QuoteArgument(game.SavePath)} {QuoteArgument(versionRemotePath)} --create-empty-src-dirs",
                cancellationToken);
            if (!versionResult.Succeeded)
            {
                return BackupOperationResult.Fail($"Failed to upload versioned backup. {CleanError(versionResult.StandardError)}");
            }

            var metadataFilePath = CreateMetadataFile(game, backupType);
            var metadataResult = await UploadMetadataAsync(game, metadataFilePath, cancellationToken);
            TryDeleteTempFile(metadataFilePath);
            if (!metadataResult.Succeeded)
            {
                return metadataResult;
            }

            game.LastBackupTime = DateTimeOffset.Now;
            _loggingService.Info($"Manual backup completed: {game.Name}; latest={latestRemotePath}; version={versionRemotePath}");
            return BackupOperationResult.Ok("Backup completed successfully.", latestRemotePath);
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Manual backup failed: {game.Name}", ex);
            return BackupOperationResult.Fail($"Backup failed: {ex.Message}");
        }
    }

    public async Task<BackupOperationResult> RestoreFromCloudAsync(GameConfig game, CancellationToken cancellationToken = default)
    {
        try
        {
            _loggingService.Info($"Manual restore started: {game.Name}");

            var validation = await ValidateGameForRcloneOperationAsync(game, requireSaveFolder: false, cancellationToken);
            if (!validation.Succeeded)
            {
                return validation;
            }

            // TODO: Check whether the configured game process is running once process monitoring exists.
            _ = await ReadCloudMetadataAsync(game, cancellationToken);

            var safetyBackup = await CreateLocalSafetyBackupAsync(game, cancellationToken);
            if (!safetyBackup.Succeeded)
            {
                return safetyBackup;
            }

            Directory.CreateDirectory(game.SavePath);
            var latestRemotePath = _rcloneService.BuildRemotePath(game.RcloneRemote, game.CloudPath, "latest");
            var restoreResult = await _rcloneService.RunRcloneCommandAsync(
                $"copy {QuoteArgument(latestRemotePath)} {QuoteArgument(game.SavePath)} --create-empty-src-dirs",
                cancellationToken);

            if (!restoreResult.Succeeded)
            {
                return BackupOperationResult.Fail($"Failed to restore from cloud. {CleanError(restoreResult.StandardError)}");
            }

            _loggingService.Info($"Manual restore completed: {game.Name}; source={latestRemotePath}; safetyBackup={safetyBackup.OutputPath}");
            return BackupOperationResult.Ok("Restore completed successfully.", safetyBackup.OutputPath);
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Manual restore failed: {game.Name}", ex);
            return BackupOperationResult.Fail($"Restore failed: {ex.Message}");
        }
    }

    public async Task<BackupOperationResult> CreateLocalSafetyBackupAsync(GameConfig game, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!IsSaveFolderValid(game))
            {
                return BackupOperationResult.Fail("Cannot create safety backup because the local save folder does not exist.");
            }

            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var targetDirectory = Path.Combine(
                appData,
                "GameSaveCloudBackup",
                "SafetyBackups",
                SanitizePathSegment(game.Name),
                $"before_restore_{CreateTimestamp()}");

            await CopyDirectoryAsync(game.SavePath, targetDirectory, cancellationToken);
            _loggingService.Info($"Safety backup created: {game.Name}; path={targetDirectory}");
            return BackupOperationResult.Ok("Safety backup created.", targetDirectory);
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to create safety backup: {game.Name}", ex);
            return BackupOperationResult.Fail($"Failed to create safety backup: {ex.Message}");
        }
    }

    public bool IsSaveFolderValid(GameConfig game)
    {
        return !string.IsNullOrWhiteSpace(game.SavePath) && Directory.Exists(game.SavePath);
    }

    public bool IsSaveFolderEmpty(GameConfig game)
    {
        if (!IsSaveFolderValid(game))
        {
            return true;
        }

        return !Directory.EnumerateFileSystemEntries(game.SavePath).Any();
    }

    public string CreateMetadataFile(GameConfig game, string backupType)
    {
        var metadata = new BackupMetadata
        {
            GameName = game.Name,
            LastBackupTime = DateTimeOffset.Now,
            SourceDevice = Environment.MachineName,
            BackupType = backupType,
            SavePath = game.SavePath,
            AppVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "0.1.0"
        };

        var tempDirectory = Path.Combine(Path.GetTempPath(), "GameSaveCloudBackup");
        Directory.CreateDirectory(tempDirectory);
        var metadataFilePath = Path.Combine(tempDirectory, $"metadata_{game.Id:N}_{CreateTimestamp()}.json");
        File.WriteAllText(metadataFilePath, JsonSerializer.Serialize(metadata, _jsonOptions));
        _loggingService.Info($"Metadata file created: {metadataFilePath}");
        return metadataFilePath;
    }

    public async Task<BackupOperationResult> UploadMetadataAsync(GameConfig game, string metadataFilePath, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(metadataFilePath))
        {
            return BackupOperationResult.Fail("Metadata file does not exist.");
        }

        var metadataRemotePath = _rcloneService.BuildRemotePath(game.RcloneRemote, game.CloudPath, "metadata.json");
        var result = await _rcloneService.RunRcloneCommandAsync(
            $"copyto {QuoteArgument(metadataFilePath)} {QuoteArgument(metadataRemotePath)}",
            cancellationToken);

        if (!result.Succeeded)
        {
            return BackupOperationResult.Fail($"Failed to upload metadata. {CleanError(result.StandardError)}");
        }

        _loggingService.Info($"Metadata uploaded: {game.Name}; remote={metadataRemotePath}");
        return BackupOperationResult.Ok("Metadata uploaded successfully.", metadataRemotePath);
    }

    public async Task<BackupMetadata?> ReadCloudMetadataAsync(GameConfig game, CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(game.RcloneRemote) || string.IsNullOrWhiteSpace(game.CloudPath))
            {
                return null;
            }

            var metadataRemotePath = _rcloneService.BuildRemotePath(game.RcloneRemote, game.CloudPath, "metadata.json");
            var metadataText = await _rcloneService.ReadRemoteTextFile(metadataRemotePath, cancellationToken);
            if (string.IsNullOrWhiteSpace(metadataText))
            {
                _loggingService.Error($"Failed metadata read or empty metadata: {game.Name}; remote={metadataRemotePath}");
                return null;
            }

            var metadata = JsonSerializer.Deserialize<BackupMetadata>(metadataText, _jsonOptions);
            _loggingService.Info($"Cloud metadata read: {game.Name}; remote={metadataRemotePath}");
            return metadata;
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to read cloud metadata: {game.Name}", ex);
            return null;
        }
    }

    private async Task<BackupOperationResult> ValidateGameForRcloneOperationAsync(GameConfig game, bool requireSaveFolder, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(game.Name))
        {
            return BackupOperationResult.Fail("Game name is required.");
        }

        if (string.IsNullOrWhiteSpace(game.RcloneRemote))
        {
            return BackupOperationResult.Fail("Rclone remote is required.");
        }

        if (string.IsNullOrWhiteSpace(game.CloudPath))
        {
            return BackupOperationResult.Fail("Cloud backup folder is required.");
        }

        if (requireSaveFolder && !IsSaveFolderValid(game))
        {
            return BackupOperationResult.Fail("Save folder does not exist.");
        }

        if (!await _rcloneService.CheckRcloneInstalled(cancellationToken))
        {
            return BackupOperationResult.Fail("rclone is not installed or is not available in PATH.");
        }

        if (!await _rcloneService.TestRemote(game.RcloneRemote, cancellationToken))
        {
            return BackupOperationResult.Fail($"Rclone remote '{game.RcloneRemote}' is missing, unavailable, or not authenticated.");
        }

        return BackupOperationResult.Ok("Validation passed.");
    }

    private static async Task CopyDirectoryAsync(string sourceDirectory, string targetDirectory, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(targetDirectory);

        foreach (var directory in Directory.EnumerateDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(sourceDirectory, directory);
            Directory.CreateDirectory(Path.Combine(targetDirectory, relativePath));
        }

        foreach (var file in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativePath = Path.GetRelativePath(sourceDirectory, file);
            var targetFile = Path.Combine(targetDirectory, relativePath);
            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);

            await using var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 81920, useAsync: true);
            await using var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await sourceStream.CopyToAsync(targetStream, cancellationToken);
        }
    }

    private static string QuoteArgument(string value)
    {
        return $"\"{value.Replace("\"", "\\\"")}\"";
    }

    private static string CreateTimestamp()
    {
        return DateTimeOffset.Now.ToString("yyyyMMdd_HHmmss");
    }

    private static string SanitizePathSegment(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sanitized = new string(value.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(sanitized) ? "UnknownGame" : sanitized;
    }

    private static string CleanError(string stderr)
    {
        return string.IsNullOrWhiteSpace(stderr) ? "No error details were returned." : stderr.Trim();
    }

    private static void TryDeleteTempFile(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        catch
        {
            // Best-effort temp cleanup only.
        }
    }
}

public sealed record BackupOperationResult(bool Succeeded, string Message, string? OutputPath = null)
{
    public static BackupOperationResult Ok(string message, string? outputPath = null) => new(true, message, outputPath);

    public static BackupOperationResult Fail(string message, string? outputPath = null) => new(false, message, outputPath);
}
