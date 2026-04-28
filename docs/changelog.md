# Changelog

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
