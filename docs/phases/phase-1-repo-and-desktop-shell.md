# Phase 1: Repo and Desktop Shell

## Goal

Create the initial C# Windows desktop app shell.

## Technology choice

WinForms was selected for the MVP shell because it is straightforward, maintainable, and enough for the first version of this utility app.

## Delivered

- C# WinForms project at `src/GameSaveCloudBackup`.
- Main window with:
  - App title
  - Add Game button
  - Placeholder rclone status area
  - Game list area
  - Recent logs/status area
- Add/Edit Game form with:
  - Game Name
  - Game EXE/Launcher path and Browse button
  - Save Folder and Browse button
  - Rclone Remote placeholder textbox
  - Cloud Backup Folder
  - Auto Backup checkbox, default true
  - Backup interval number input, default 10
  - Backup on Close checkbox, default true
  - Save and Cancel buttons
- Models:
  - `AppConfig`
  - `GameConfig`
  - `BackupMetadata`
- Services:
  - `ConfigService`
  - `LoggingService`
- Local config at `%LOCALAPPDATA%/GameSaveCloudBackup/config.json`.
- Local logs at `%LOCALAPPDATA%/GameSaveCloudBackup/Logs/app.log`.
- Manual game management foundation:
  - Add game
  - Edit game
  - Remove game
  - Refresh list after changes
- GitHub Actions Windows build workflow.

## Explicitly not included

- No rclone integration.
- No real backup logic.
- No real restore logic.
- No game process monitoring.

## Status

Completed.

## Next phase

Phase 2: rclone integration.
