# Game Save Cloud Backup Manager

Game Save Cloud Backup Manager is a planned Windows desktop application for managing game save backups to cloud storage through [rclone](https://rclone.org/).

Users will be able to add games, select each game's EXE or launcher, choose the save folder, choose an rclone remote and cloud folder, then run manual or automatic backups and restores.

## Current status

Planning stage. No application code exists yet.

This repository is being used first as durable project memory: product planning, architecture, roadmap, risks, research notes, phase plans, and decision records before implementation begins. Sensible, because software without memory becomes archaeology with buttons.

## MVP summary

Initial MVP target: a C# Windows desktop application using WPF or WinForms.

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
