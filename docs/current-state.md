# Current State

- Phase 5 game monitoring and automatic backup is completed.
- Repository contains a runnable C# WinForms desktop application under `src/GameSaveCloudBackup`.
- The app includes game management, local JSON config, logging, rclone integration, manual backup/restore, startup restore prompt, process monitoring, automatic running-game backup, and final close backup.
- Config is stored at `%LOCALAPPDATA%/GameSaveCloudBackup/config.json`.
- Logs are stored at `%LOCALAPPDATA%/GameSaveCloudBackup/Logs/app.log`.
- Rclone availability is checked on startup using `rclone version`.
- Each game row includes Running / Not Running monitor status, Auto Backup enabled state, interval, backup-running state, Last Auto Backup, Last Backup, Backup Now, Restore from Cloud, and Open Save Folder.
- `GameMonitorService` monitors configured games by deriving the process name from `exePath` and checking every few seconds.
- When a game starts, the app logs it, waits about one minute, then runs automatic backups every configured interval while the game remains running.
- When a game closes, the app logs it, waits about five seconds, then runs one final close backup if `backupOnClose` is enabled.
- Automatic backup calls `BackupService.BackupNowAsync(game, "auto")`.
- Final close backup calls `BackupService.BackupNowAsync(game, "close")`.
- Per-game overlap protection prevents simultaneous backups for the same game.
- Auto backup skips and logs missing save folders, empty save folders, invalid EXE paths, rclone failures, and backup failures.
- The app does not store rclone credentials or cloud provider credentials.
- GitHub Actions build workflow exists for Windows.
- Next recommended step: Phase 6, polish, packaging, and reliability.
