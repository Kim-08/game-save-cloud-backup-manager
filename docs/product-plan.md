# Product Plan

## Product

Game Save Cloud Backup Manager is a Windows desktop app that helps users protect game save files by backing them up to cloud storage through rclone.

## Target user

PC players who want cloud-like save backup for games that do not provide reliable built-in cloud saves, or who want an extra backup layer.

## Core workflow

1. User adds a game.
2. User chooses the game EXE or launcher.
3. User chooses the save folder.
4. User chooses an rclone remote.
5. User chooses a cloud folder.
6. On app startup, the app checks if cloud backup metadata exists.
7. If the cloud save exists and is newer, the app asks whether to restore.
8. After the startup restore prompt, the app does not auto-restore again in the same session unless the user manually chooses Restore.
9. While a game is running, the app backs up every 10 minutes by default.
10. When the game closes, the app performs one final backup.

## App responsibilities

- UI
- Game list management
- Local JSON config
- rclone command execution
- Cloud backup metadata
- Startup restore prompt
- Manual backup
- Manual restore
- Game process monitoring
- Auto backup while game is running
- Final backup when the game closes
- Logs and error handling

## Non-goals for initial implementation

- Direct Google Drive, Dropbox, OneDrive, or other cloud API integrations
- Multiplayer save conflict resolution
- Real-time file syncing
- Background Windows service
- Account system
