# Game Save Cloud Backup Manager

Game Save Cloud Backup Manager is a Windows desktop application for managing game save backups to cloud storage through [rclone](https://rclone.org/).

Users will be able to add games, select each game's EXE or launcher, choose the save folder, choose an rclone remote and cloud folder, then run manual or automatic backups and restores.

## Current status

Phase 2 completed: the repository contains a runnable C# WinForms desktop shell with local JSON config, logging, game management, rclone detection, rclone version display, remote listing, and safe async rclone command execution foundation.

No real backup/restore workflows or automatic game monitoring are implemented yet. The app can see the cave entrance. It has not gone spelunking with user saves yet, which is healthy survival behavior.

## MVP summary

Initial MVP stack: C# / .NET 8 / WinForms.

The MVP should support:

- Local game list management
- Local JSON configuration
- rclone command execution
- Manual backup
- Manual restore
- Startup restore prompt when cloud backup metadata is newer
- Game process monitoring
- Automatic backup while a game is running, every 10 minutes by default
- Final backup when a game closes
- Logs and error handling

Implemented so far:

- Desktop UI shell
- Add/edit/remove game management
- Local config file at `%LOCALAPPDATA%/GameSaveCloudBackup/config.json`
- Local logs at `%LOCALAPPDATA%/GameSaveCloudBackup/Logs/app.log`
- rclone availability check using `rclone version`
- rclone remote listing using `rclone listremotes`
- Remote validation helper
- Remote text file read helper
- Safe remote path builder

## Running locally

Install the .NET 8 SDK on Windows, then run:

```bash
dotnet run --project src/GameSaveCloudBackup/GameSaveCloudBackup.csproj
```

## rclone setup

This app uses rclone as the cloud engine. It does not manage, store, or sync cloud credentials directly.

### Install rclone

Download and install rclone from:

<https://rclone.org/downloads/>

After installation, make sure `rclone` is available in your PATH:

```bash
rclone version
```

### Configure rclone

Run:

```bash
rclone config
```

Follow the prompts to create a cloud remote.

### Example: Google Drive remote

A common setup is a Google Drive remote named `gdrive`:

```text
name> gdrive
Storage> drive
```

Then follow rclone's browser-based authorization prompts.

Verify the remote exists:

```bash
rclone listremotes
```

Expected example output:

```text
gdrive:
```

In the app, use remote name:

```text
gdrive
```

The app does not store Google Drive, Dropbox, OneDrive, or other provider credentials. Those remain in rclone's own config.

## Documentation

- [Documentation index](docs/index.md)
- [Product plan](docs/product-plan.md)
- [Architecture](docs/architecture.md)
- [Roadmap](docs/roadmap.md)
- [Current state](docs/current-state.md)
- [Project memory](docs/project-memory.md)
- [Changelog](docs/changelog.md)
- [Risks](docs/risks.md)
- [rclone research](docs/research-rclone.md)

## Important design choices

- Use rclone as the cloud engine.
- Do not directly integrate Google Drive, Dropbox, OneDrive, or other cloud provider APIs.
- Use `rclone copy` by default.
- Do not use `rclone sync` by default.
- Start with safe one-way backup and manual restore.
- Store app configuration under `%LOCALAPPDATA%`.

## Planned cloud folder structure

```text
GameSaveBackups/
└── Game Name/
    ├── latest/
    ├── versions/
    └── metadata.json
```

Example metadata:

```json
{
  "gameName": "Stardew Valley",
  "lastBackupTime": "2026-04-28T10:30:00+08:00",
  "sourceDevice": "USER-PC",
  "backupType": "auto",
  "savePath": "C:\\Users\\User\\AppData\\Roaming\\StardewValley\\Saves",
  "appVersion": "1.0.0"
}
```

Example remote paths:

```text
gdrive:GameSaveBackups/Stardew Valley/latest
gdrive:GameSaveBackups/Stardew Valley/metadata.json
```
