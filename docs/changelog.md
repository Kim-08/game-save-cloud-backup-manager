# Changelog

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
