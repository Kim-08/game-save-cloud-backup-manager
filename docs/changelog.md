# Changelog

## 2026-04-28 - MVP stabilization pass

- Reworked rclone execution to use `ProcessStartInfo.ArgumentList` instead of shell-style quoted command strings, reducing path/space/quote bugs.
- Added safe relative cloud-path validation in both Add/Edit Game and backup/restore validation.
- Blocked restore when the configured game process appears to be running.
- Made log viewer reads tail-limited so large log files are less likely to freeze the UI.
- Made config writes use a temporary file and overwrite move, and made corrupt-config backups use millisecond timestamps to avoid collisions.
- Improved backup/restore lock handling so cancellation before lock acquisition does not release an unheld semaphore.
- Added the required README `MVP checklist` section.
- Re-ran build and publish validation in the .NET 8 SDK container.

## 2026-04-28 - Phase 6 polish, packaging, and reliability

- Added a better in-app logs viewer with refresh, Open Logs Folder, and Open Config Folder buttons.
- Added rclone setup help in the UI and expanded README setup/publish instructions.
- Added friendly empty state when no games are configured.
- Improved Add/Edit Game validation for EXE path, save folder, remote name, cloud folder, startup restore prompt, and version retention.
- Added backup history display from managed timestamped cloud `versions/` folders.
- Added safe retention for versioned backups: keep latest `MaxVersionBackups`, with `0` meaning keep all, and prune only app-managed timestamp folders.
- Added corrupt config recovery by backing up invalid JSON to `config.bad.TIMESTAMP.json` and creating a fresh config.
- Hardened logging so locked/unavailable log files do not crash the app.
- Added defensive rclone cancellation handling, process-tree kill on cancellation, and sensitive argument redaction in logs.
- Improved UI async error handling and app-level exception logging.
- Ensured game monitoring and pending close backups are canceled during app shutdown.
- Added `scripts/publish-windows.ps1` for Windows Release publish output.
- Updated GitHub Actions with publish validation.
- Updated README, current state, project memory, roadmap, Phase 6 docs, and risks.
- Marked MVP complete for early local use.

## 2026-04-28 - Phase 5 game monitoring and automatic backup

- Added `GameMonitorService` for process monitoring based on each game EXE/launcher path.
- Added runtime UI columns for Running / Not Running, Auto Backup, interval, backup-running state, and Last Auto Backup.
- Added automatic backup after game start and repeated interval backups while running.
- Added final close backup after a game exits when `backupOnClose` is enabled.
- Added per-game overlap protection to prevent simultaneous backups for the same game.
- Added skip/error logging for invalid EXE path, missing save folder, empty save folder, rclone failures, backup failures, and overlapping backup attempts.
- Updated README, current state, project memory, and Phase 5 docs.
- Set next recommended step to Phase 6: polish, packaging, and reliability.

## 2026-04-28 - Startup restore prompt

- Added startup cloud restore checks for games with `startupRestorePrompt` enabled.
- Added `RestorePromptDialog` with Restore from Cloud, Keep Local Save, and Ask Later choices.
- Added in-memory per-session tracking for games already checked and prompted.
- Startup check reads cloud `metadata.json`, compares cloud backup time against local save folder modified time, and prompts only when cloud appears newer or local saves are missing.
- Startup restore uses the existing safe `BackupService.RestoreFromCloudAsync` path with local safety backup before restore.
- Missing rclone, missing/invalid metadata, and older cloud metadata are logged and skipped without crashing.
- Updated README, current state, project memory, and Phase 4 docs.
- Set next recommended step to Phase 5: game monitoring and automatic backup.

## 2026-04-28 - Manual backup and restore

- Added `BackupService` for manual backup, manual restore, safety backups, save folder validation, empty save folder rejection, metadata creation/upload/read, and restore support.
- Added Backup Now, Restore from Cloud, Open Save Folder, and Last Backup display to the game list UI.
- Manual Backup Now now copies saves to `latest/`, copies a versioned backup to `versions/<TIMESTAMP>/`, uploads `metadata.json`, updates local `lastBackupTime`, and logs results.
- Manual Restore from Cloud now reads cloud metadata when available, asks for confirmation, creates a local safety backup, restores cloud `latest/`, and logs results.
- Updated README, current state, project memory, and phase docs.
- Set next recommended step to Phase 5: startup restore prompt and game monitoring.

## 2026-04-28 - Phase 2

- Added `RcloneService` with async command execution, rclone availability checks, version lookup, remote listing, remote validation, remote text reads, and remote path building.
- Updated main UI to show rclone installed/missing status, version, and configured remotes.
- Updated Add/Edit Game UI to select or enter rclone remotes and test remote access.
- Added rclone setup instructions to README.
- Updated rclone research notes, project memory, current state, and Phase 2 documentation.
- Marked Phase 2 complete and set Phase 3 manual backup and restore as the next step.

## 2026-04-28 - Phase 1

- Implemented C# WinForms desktop application shell.
- Added main window, add/edit game form, game list management, config service, logging service, and app models.
- Added local config storage at `%LOCALAPPDATA%/GameSaveCloudBackup/config.json`.
- Added local log storage at `%LOCALAPPDATA%/GameSaveCloudBackup/Logs/app.log`.
- Added Windows GitHub Actions build workflow.
- Marked Phase 1 complete and set Phase 2 rclone integration as the next step.

## 2026-04-28

- Initialized repository as planning and project memory for Game Save Cloud Backup Manager.
- Added product plan, architecture, roadmap, current state, project memory, risks, rclone research, phase plans, and decision records.
- Added MIT license and C#/.NET/Visual Studio `.gitignore`.
