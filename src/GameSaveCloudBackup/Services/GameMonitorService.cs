using System.Collections.Concurrent;
using System.Diagnostics;
using GameSaveCloudBackup.Models;

namespace GameSaveCloudBackup.Services;

public sealed class GameMonitorService : IDisposable
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan FirstAutoBackupDelay = TimeSpan.FromMinutes(1);
    private static readonly TimeSpan CloseBackupDelay = TimeSpan.FromSeconds(5);

    private readonly BackupService _backupService;
    private readonly LoggingService _loggingService;
    private readonly ConcurrentDictionary<Guid, MonitorEntry> _entries = new();
    private CancellationTokenSource? _monitorCts;
    private Task? _monitorTask;

    public event EventHandler<GameMonitorStateChangedEventArgs>? StateChanged;

    public GameMonitorService(BackupService backupService, LoggingService loggingService)
    {
        _backupService = backupService;
        _loggingService = loggingService;
    }

    public void Start(IEnumerable<GameConfig> games)
    {
        UpdateGames(games);
        if (_monitorTask is { IsCompleted: false })
        {
            return;
        }

        _monitorCts = new CancellationTokenSource();
        _monitorTask = Task.Run(() => MonitorLoopAsync(_monitorCts.Token));
        _loggingService.Info("Game monitor started");
    }

    public void UpdateGames(IEnumerable<GameConfig> games)
    {
        var currentIds = games.Select(game => game.Id).ToHashSet();
        foreach (var staleId in _entries.Keys.Where(id => !currentIds.Contains(id)).ToList())
        {
            if (_entries.TryRemove(staleId, out var removed))
            {
                removed.RunningBackupCts?.Cancel();
                _loggingService.Info($"Game monitor removed game: {removed.Game.Name}");
            }
        }

        foreach (var game in games)
        {
            _entries.AddOrUpdate(
                game.Id,
                id =>
                {
                    var entry = new MonitorEntry(game);
                    ApplyStateToGame(entry, "Not Running");
                    return entry;
                },
                (_, existing) =>
                {
                    existing.Game = game;
                    return existing;
                });
        }
    }

    public void Stop()
    {
        _monitorCts?.Cancel();
        foreach (var entry in _entries.Values)
        {
            entry.RunningBackupCts?.Cancel();
        }

        try
        {
            if (_monitorTask is { IsCompleted: false })
            {
                _monitorTask.Wait(TimeSpan.FromSeconds(2));
            }
        }
        catch (Exception ex) when (ex is AggregateException or OperationCanceledException)
        {
            // Expected during shutdown. Background work has been asked to stop.
        }

        _loggingService.Info("Game monitor stopped");
    }

    private async Task MonitorLoopAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(PollInterval);
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                foreach (var entry in _entries.Values.ToList())
                {
                    await CheckGameAsync(entry, cancellationToken);
                }

                await timer.WaitForNextTickAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _loggingService.Error("Game monitor loop error", ex);
            }
        }
    }

    private async Task CheckGameAsync(MonitorEntry entry, CancellationToken cancellationToken)
    {
        var running = IsGameProcessRunning(entry);
        if (running && !entry.IsRunning)
        {
            await HandleGameStartedAsync(entry, cancellationToken);
        }
        else if (!running && entry.IsRunning)
        {
            HandleGameClosed(entry);
        }
        else
        {
            ApplyStateToGame(entry, running ? "Running" : entry.LastStatusMessage);
        }
    }

    private Task HandleGameStartedAsync(MonitorEntry entry, CancellationToken cancellationToken)
    {
        lock (entry.SyncRoot)
        {
            entry.IsRunning = true;
            entry.LastStatusMessage = "Running";
            entry.RunningBackupCts?.Cancel();
            entry.RunningBackupCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        }

        _loggingService.Info($"Game started: {entry.Game.Name}");
        ApplyStateToGame(entry, "Running");
        _ = Task.Run(() => RunningAutoBackupLoopAsync(entry, entry.RunningBackupCts.Token), CancellationToken.None);
        return Task.CompletedTask;
    }

    private void HandleGameClosed(MonitorEntry entry)
    {
        lock (entry.SyncRoot)
        {
            entry.IsRunning = false;
            entry.LastStatusMessage = "Not Running";
            entry.RunningBackupCts?.Cancel();
            entry.RunningBackupCts = null;
        }

        _loggingService.Info($"Game closed: {entry.Game.Name}");
        ApplyStateToGame(entry, "Not Running");

        if (entry.Game.BackupOnClose)
        {
            var shutdownToken = _monitorCts?.Token ?? CancellationToken.None;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(CloseBackupDelay, shutdownToken);
                    await RunBackupIfAllowedAsync(entry, "close", shutdownToken);
                }
                catch (OperationCanceledException)
                {
                    _loggingService.Info($"close backup canceled: {entry.Game.Name}");
                }
            }, CancellationToken.None);
        }
    }

    private async Task RunningAutoBackupLoopAsync(MonitorEntry entry, CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(FirstAutoBackupDelay, cancellationToken);
            while (!cancellationToken.IsCancellationRequested)
            {
                if (entry.Game.AutoBackup && entry.IsRunning)
                {
                    await RunBackupIfAllowedAsync(entry, "auto", cancellationToken);
                }
                else if (!entry.Game.AutoBackup)
                {
                    _loggingService.Info($"Auto backup skipped because it is disabled: {entry.Game.Name}");
                    ApplyStateToGame(entry, entry.IsRunning ? "Running" : "Not Running");
                }

                var interval = TimeSpan.FromMinutes(Math.Max(1, entry.Game.BackupIntervalMinutes));
                await Task.Delay(interval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when the game closes or monitoring stops.
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Auto backup loop failed: {entry.Game.Name}", ex);
        }
    }

    private async Task RunBackupIfAllowedAsync(MonitorEntry entry, string backupType, CancellationToken cancellationToken = default)
    {
        lock (entry.SyncRoot)
        {
            if (entry.IsBackupRunning)
            {
                _loggingService.Info($"{backupType} backup skipped because another backup is already running: {entry.Game.Name}");
                return;
            }

            entry.IsBackupRunning = true;
        }

        ApplyStateToGame(entry, entry.IsRunning ? "Running" : "Not Running");
        try
        {
            if (!_backupService.IsSaveFolderValid(entry.Game))
            {
                _loggingService.Error($"{backupType} backup skipped because save folder is missing: {entry.Game.Name}");
                return;
            }

            if (_backupService.IsSaveFolderEmpty(entry.Game))
            {
                _loggingService.Error($"{backupType} backup skipped because save folder is empty: {entry.Game.Name}");
                return;
            }

            var result = await _backupService.BackupNowAsync(entry.Game, backupType, cancellationToken);
            if (result.Succeeded)
            {
                entry.LastAutoBackupTime = DateTimeOffset.Now;
                _loggingService.Info($"{backupType} backup completed: {entry.Game.Name}");
            }
            else
            {
                _loggingService.Error($"{backupType} backup failed: {entry.Game.Name}; {result.Message}");
            }
        }
        catch (OperationCanceledException)
        {
            _loggingService.Info($"{backupType} backup canceled: {entry.Game.Name}");
        }
        catch (Exception ex)
        {
            _loggingService.Error($"{backupType} backup failed: {entry.Game.Name}", ex);
        }
        finally
        {
            lock (entry.SyncRoot)
            {
                entry.IsBackupRunning = false;
            }

            ApplyStateToGame(entry, entry.IsRunning ? "Running" : "Not Running");
        }
    }

    private bool IsGameProcessRunning(MonitorEntry entry)
    {
        var game = entry.Game;
        if (string.IsNullOrWhiteSpace(game.ExePath))
        {
            SetInvalidState(entry, "Not Running (missing EXE path)");
            return false;
        }

        if (!File.Exists(game.ExePath))
        {
            SetInvalidState(entry, "Not Running (invalid EXE path)");
            return false;
        }

        var processName = Path.GetFileNameWithoutExtension(game.ExePath);
        if (string.IsNullOrWhiteSpace(processName))
        {
            SetInvalidState(entry, "Not Running (invalid process name)");
            return false;
        }

        try
        {
            var isRunning = Process.GetProcessesByName(processName).Any();
            if (!isRunning)
            {
                entry.LastStatusMessage = "Not Running";
            }

            return isRunning;
        }
        catch (Exception ex)
        {
            _loggingService.Error($"Process lookup failed: {game.Name}; process={processName}", ex);
            entry.LastStatusMessage = "Not Running (process lookup failed)";
            return false;
        }
    }

    private void SetInvalidState(MonitorEntry entry, string status)
    {
        if (!string.Equals(entry.LastStatusMessage, status, StringComparison.Ordinal))
        {
            _loggingService.Error($"Game monitor skipped {entry.Game.Name}: {status}");
        }

        entry.LastStatusMessage = status;
    }

    private void ApplyStateToGame(MonitorEntry entry, string? status = null)
    {
        var newStatus = status ?? (entry.IsRunning ? "Running" : entry.LastStatusMessage);
        var newIntervalDisplay = $"{Math.Max(1, entry.Game.BackupIntervalMinutes)} min";

        var changed =
            !string.Equals(entry.LastEmittedStatus, newStatus, StringComparison.Ordinal) ||
            entry.LastEmittedBackupRunning != entry.IsBackupRunning ||
            entry.LastEmittedLastAutoBackupTime != entry.LastAutoBackupTime ||
            !string.Equals(entry.LastEmittedIntervalDisplay, newIntervalDisplay, StringComparison.Ordinal);

        entry.Game.MonitorStatus = newStatus;
        entry.Game.IsBackupRunning = entry.IsBackupRunning;
        entry.Game.LastAutoBackupTime = entry.LastAutoBackupTime;
        entry.Game.AutoBackupIntervalDisplay = newIntervalDisplay;

        if (!changed)
        {
            return;
        }

        entry.LastEmittedStatus = newStatus;
        entry.LastEmittedBackupRunning = entry.IsBackupRunning;
        entry.LastEmittedLastAutoBackupTime = entry.LastAutoBackupTime;
        entry.LastEmittedIntervalDisplay = newIntervalDisplay;
        StateChanged?.Invoke(this, new GameMonitorStateChangedEventArgs(entry.Game.Id));
    }

    public void Dispose()
    {
        Stop();
        _monitorCts?.Dispose();
        foreach (var entry in _entries.Values)
        {
            entry.RunningBackupCts?.Dispose();
        }
    }

    private sealed class MonitorEntry
    {
        public MonitorEntry(GameConfig game)
        {
            Game = game;
        }

        public object SyncRoot { get; } = new();
        public GameConfig Game { get; set; }
        public bool IsRunning { get; set; }
        public bool IsBackupRunning { get; set; }
        public DateTimeOffset? LastAutoBackupTime { get; set; }
        public string LastStatusMessage { get; set; } = "Not Running";
        public string? LastEmittedStatus { get; set; }
        public bool LastEmittedBackupRunning { get; set; }
        public DateTimeOffset? LastEmittedLastAutoBackupTime { get; set; }
        public string? LastEmittedIntervalDisplay { get; set; }
        public CancellationTokenSource? RunningBackupCts { get; set; }
    }
}

public sealed class GameMonitorStateChangedEventArgs : EventArgs
{
    public GameMonitorStateChangedEventArgs(Guid gameId)
    {
        GameId = gameId;
    }

    public Guid GameId { get; }
}
