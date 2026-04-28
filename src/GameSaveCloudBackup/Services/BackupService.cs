using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameSaveCloudBackup.Models;

namespace GameSaveCloudBackup.Services;

public sealed class BackupService
{
    private static readonly Regex TimestampVersionPattern = new("^\\d{8}_\\d{6}$", RegexOptions.Compiled);

    private readonly RcloneService _rcloneService;
    private readonly LoggingService _loggingService;
    private readonly ConcurrentDictionary<Guid, SemaphoreSlim> _operationLocks = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public BackupService(RcloneService rcloneService, LoggingService loggingService)
    {
        _rcloneService = rcloneService;
        _loggingService = loggingService;
    }

    public async Task<BackupOperationResult> BackupNowAsync(GameConfig game, string backupType, CancellationToken cancellationToken = default)
    {
        var operationLock = _operationLocks.GetOrAdd(game.Id, _ => new SemaphoreSlim(1, 1));
        var lockTaken = false;

        try
        {
            lockTaken = await operationLock.WaitAsync(0, cancellationToken);
            if (!lockTaken)
            {
                _loggingService.Info($"{backupType} backup skipped because another backup or restore is already running: {game.Name}");
                return BackupOperationResult.Fail("Another backup or restore is already running for this game.");
            }

            _loggingService.Info($"{backupType} backup started: {game.Name}");

            var validation = await ValidateGameForRcloneOperationAsync(game, requireSaveFolder: true, cancellationToken);
            if (!validation.Succeeded)
            {
                return validation;
            }

            if (IsSaveFolderEmpty(game))
            {
                return BackupOperationResult.Fail("Save folder is empty. Backup was rejected to avoid uploading an empty save set.");
            }

            var versionTimestamp = CreateTimestamp();
            var latestRemotePath = _rcloneService.BuildRemotePath(game.RcloneRemote, game.CloudPath, "latest");
            var versionRemotePath = _rcloneService.BuildRemotePath(game.RcloneRemote, game.CloudPath, $"versions/{versionTimestamp}");

            var latestResult = await _rcloneService.RunRcloneCommandAsync(
                ["copy", game.SavePath, latestRemotePath, "--create-empty-src-dirs"],
                cancellationToken);
            if (!latestResult.Succeeded)
            {
                return BackupOperationResult.Fail($"Failed to upload latest backup. {CleanError(latestResult.StandardError)}");
            }

            var versionResult = await _rcloneService.RunRcloneCommandAsync(
                ["copy", game.SavePath, versionRemotePath, "--create-empty-src-dirs"],
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

            await PruneOldVersionBackupsAsync(game, cancellationToken);

            game.LastBackupTime = DateTimeOffset.Now;
            _loggingService.Info($"{backupType} backup completed: {game.Name}; latest={latestRemotePath}; version={versionRemotePath}");
            return BackupOperationResult.Ok("Backup completed successfully.", latestRemotePath);
        }
        catch (OperationCanceledException)
        {
            _loggingService.Info($"{backupType} backup canceled: {game.Name}");
            return BackupOperationResult.Fail("Backup was canceled.");
        }
        catch (Exception ex)
        {
            _loggingService.Error($"{backupType} backup failed: {game.Name}", ex);
            return BackupOperationResult.Fail($"Backup failed: {ex.Message}");
        }
        finally
        {
            if (lockTaken)
            {
                operationLock.Release();
            }
        }
    }

    public async Task<BackupOperationResult> RestoreFromCloudAsync(GameConfig game, CancellationToken cancellationToken = default)
    {
        var operationLock = _operationLocks.GetOrAdd(game.Id, _ => new SemaphoreSlim(1, 1));
        var lockTaken = false;

        try
        {
            lockTaken = await operationLock.WaitAsync(0, cancellationToken);
            if (!lockTaken)
            {
                _loggingService.Info($"Restore skipped because another backup or restore is already running: {game.Name}");
                return BackupOperationResult.Fail("Another backup or restore is already running for this game.");
            }

            _loggingService.Info($"Manual restore started: {game.Name}");

            var validation = await ValidateGameForRcloneOperationAsync(game, requireSaveFolder: false, cancellationToken);
            if (!validation.Succeeded)
            {
                return validation;
            }

            var closedCheck = EnsureConfiguredGameProcessIsNotRunning(game);
            if (!closedCheck.Succeeded)
            {
                return closedCheck;
            }

            _ = await ReadCloudMetadataAsync(game, cancellationToken);

            var safetyBackup = await CreateLocalSafetyBackupAsync(game, cancellationToken);
            if (!safetyBackup.Succeeded)
            {
                return safetyBackup;
            }

            Directory.CreateDirectory(game.SavePath);
            var latestRemotePath = _rcloneService.BuildRemotePath(game.RcloneRemote, game.CloudPath, "latest");
            var restoreResult = await _rcloneService.RunRcloneCommandAsync(
                ["copy", latestRemotePath, game.SavePath, "--create-empty-src-dirs"],
                cancellationToken);

            if (!restoreResult.Succeeded)
            {
                return BackupOperationResult.Fail($"Failed to restore from cloud. {CleanError(restoreResult.StandardError)}");
            }

            _loggingService.Info($"Manual restore completed: {game.Name}; source={latestRemotePath}; safetyBackup={safetyBackup.OutputPath}");
            return BackupOperationResult.Ok("Restore completed successfully.", safetyBackup.OutputPath);
        }
        catch (OperationCanceledException)
        {
            _loggingService.Info($"Manual restore canceled: {game.Name}");
            return BackupOperationResult.Fail("Restore was canceled.");
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Manual restore failed: {game.Name}", ex);
            return BackupOperationResult.Fail($"Restore failed: {ex.Message}");
        }
        finally
        {
            if (lockTaken)
            {
                operationLock.Release();
            }
        }
    }

    public async Task<BackupOperationResult> CreateLocalSafetyBackupAsync(GameConfig game, CancellationToken cancellationToken = default)
    {
        try
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            var targetDirectory = Path.Combine(
                appData,
                "GameSaveCloudBackup",
                "SafetyBackups",
                SanitizePathSegment(game.Name),
                $"before_restore_{CreateTimestamp()}");

            if (!IsSaveFolderValid(game))
            {
                Directory.CreateDirectory(targetDirectory);
                _loggingService.Info($"Safety backup skipped because local save folder does not exist: {game.Name}; reservedPath={targetDirectory}");
                return BackupOperationResult.Ok("Local save folder did not exist, so there was nothing to safety-backup.", targetDirectory);
            }

            await CopyDirectoryAsync(game.SavePath, targetDirectory, cancellationToken);
            _loggingService.Info($"Safety backup created: {game.Name}; path={targetDirectory}");
            return BackupOperationResult.Ok("Safety backup created.", targetDirectory);
        }
        catch (OperationCanceledException)
        {
            _loggingService.Info($"Safety backup canceled: {game.Name}");
            return BackupOperationResult.Fail("Safety backup was canceled.");
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to create safety backup: {game.Name}", ex);
            return BackupOperationResult.Fail($"Failed to create safety backup: {ex.Message}");
        }
    }

    public async Task<IReadOnlyList<BackupHistoryEntry>> GetBackupHistoryAsync(GameConfig game, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(game.RcloneRemote) || string.IsNullOrWhiteSpace(game.CloudPath))
        {
            return [];
        }

        var versionsRemotePath = _rcloneService.BuildRemotePath(game.RcloneRemote, game.CloudPath, "versions");
        var versionNames = await _rcloneService.ListRemoteDirectories(versionsRemotePath, cancellationToken);
        return versionNames
            .Where(IsManagedVersionDirectory)
            .Select(name => new BackupHistoryEntry(name, TryParseVersionTimestamp(name), _rcloneService.BuildRemotePath(game.RcloneRemote, game.CloudPath, $"versions/{name}")))
            .OrderByDescending(entry => entry.CreatedAt ?? DateTimeOffset.MinValue)
            .ToList();
    }

    public DateTimeOffset? GetLocalSaveLastModified(GameConfig game)
    {
        if (!IsSaveFolderValid(game))
        {
            return null;
        }

        try
        {
            var latestWrite = Directory
                .EnumerateFileSystemEntries(game.SavePath, "*", SearchOption.AllDirectories)
                .Select(path => File.Exists(path) ? File.GetLastWriteTimeUtc(path) : Directory.GetLastWriteTimeUtc(path))
                .DefaultIfEmpty(Directory.GetLastWriteTimeUtc(game.SavePath))
                .Max();
            return new DateTimeOffset(latestWrite, TimeSpan.Zero).ToLocalTime();
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to determine local save last modified time: {game.Name}", ex);
            return null;
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

        try
        {
            return !Directory.EnumerateFileSystemEntries(game.SavePath).Any();
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to inspect save folder contents: {game.Name}", ex);
            return true;
        }
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
            ["copyto", metadataFilePath, metadataRemotePath],
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
        catch (OperationCanceledException)
        {
            _loggingService.Info($"Cloud metadata read canceled: {game.Name}");
            return null;
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

        if (game.RcloneRemote.Contains(':'))
        {
            return BackupOperationResult.Fail("Rclone remote should be only the remote name, for example 'gdrive', not 'gdrive:folder'.");
        }

        if (string.IsNullOrWhiteSpace(game.CloudPath))
        {
            return BackupOperationResult.Fail("Cloud backup folder is required.");
        }

        if (game.CloudPath.Contains(':'))
        {
            return BackupOperationResult.Fail("Cloud backup folder should not include the remote name or colon.");
        }

        if (!IsSafeCloudPath(game.CloudPath))
        {
            return BackupOperationResult.Fail("Cloud backup folder must be a relative folder path and cannot contain '.' or '..' path segments.");
        }

        if (string.IsNullOrWhiteSpace(game.SavePath))
        {
            return BackupOperationResult.Fail("Save folder path is required.");
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

    private async Task PruneOldVersionBackupsAsync(GameConfig game, CancellationToken cancellationToken)
    {
        if (game.MaxVersionBackups <= 0)
        {
            _loggingService.Info($"Version retention disabled: {game.Name}");
            return;
        }

        var history = await GetBackupHistoryAsync(game, cancellationToken);
        var staleVersions = history.Skip(game.MaxVersionBackups).ToList();
        foreach (var version in staleVersions)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IsManagedVersionDirectory(version.Name))
            {
                continue;
            }

            var result = await _rcloneService.DeleteRemoteDirectory(version.RemotePath, cancellationToken);
            if (result.Succeeded)
            {
                _loggingService.Info($"Pruned old versioned backup: {game.Name}; version={version.Name}");
            }
            else
            {
                _loggingService.Error($"Failed to prune old versioned backup: {game.Name}; version={version.Name}; {CleanError(result.StandardError)}");
            }
        }
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

            await using var sourceStream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 81920, useAsync: true);
            await using var targetStream = new FileStream(targetFile, FileMode.Create, FileAccess.Write, FileShare.None, 81920, useAsync: true);
            await sourceStream.CopyToAsync(targetStream, cancellationToken);
        }
    }

    private static bool IsManagedVersionDirectory(string value)
    {
        return TimestampVersionPattern.IsMatch(value.Trim().TrimEnd('/'));
    }

    private static DateTimeOffset? TryParseVersionTimestamp(string value)
    {
        return DateTimeOffset.TryParseExact(value, "yyyyMMdd_HHmmss", null, System.Globalization.DateTimeStyles.AssumeLocal, out var parsed)
            ? parsed
            : null;
    }

    private BackupOperationResult EnsureConfiguredGameProcessIsNotRunning(GameConfig game)
    {
        if (string.IsNullOrWhiteSpace(game.ExePath) || !File.Exists(game.ExePath))
        {
            return BackupOperationResult.Ok("No process check was possible because the game EXE path is missing or invalid.");
        }

        var processName = Path.GetFileNameWithoutExtension(game.ExePath);
        if (string.IsNullOrWhiteSpace(processName))
        {
            return BackupOperationResult.Ok("No process check was possible because the game process name is invalid.");
        }

        try
        {
            if (Process.GetProcessesByName(processName).Any())
            {
                return BackupOperationResult.Fail($"Restore blocked because '{game.Name}' appears to be running. Close the game before restoring saves.");
            }

            return BackupOperationResult.Ok("Game is not running.");
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Failed to check whether game is running before restore: {game.Name}", ex);
            return BackupOperationResult.Fail("Restore blocked because the app could not confirm the game is closed.");
        }
    }

    private static bool IsSafeCloudPath(string cloudPath)
    {
        if (string.IsNullOrWhiteSpace(cloudPath))
        {
            return false;
        }

        var normalized = cloudPath.Trim().Replace('\\', '/').Trim('/');
        if (string.IsNullOrWhiteSpace(normalized))
        {
            return false;
        }

        return normalized
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .All(segment => segment != "." && segment != "..");
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

public sealed record BackupHistoryEntry(string Name, DateTimeOffset? CreatedAt, string RemotePath);

public sealed record BackupOperationResult(bool Succeeded, string Message, string? OutputPath = null)
{
    public static BackupOperationResult Ok(string message, string? outputPath = null) => new(true, message, outputPath);

    public static BackupOperationResult Fail(string message, string? outputPath = null) => new(false, message, outputPath);
}
