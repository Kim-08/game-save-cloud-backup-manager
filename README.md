# Game Save Cloud Backup Manager

Game Save Cloud Backup Manager is a Windows desktop application for managing game save backups to cloud storage through [rclone](https://rclone.org/).

Users can add games, select each game's EXE or launcher, choose the save folder, choose an rclone remote and cloud folder, then run manual backups and restores. Automatic game-running backup is still intentionally not implemented yet. The monster remains in the next room.

## Current status

Manual backup, manual restore, and startup cloud restore prompt are implemented.

The app currently supports:

- C# / .NET 8 / WinForms desktop shell
- Add/edit/remove game management
- Local config file at `%LOCALAPPDATA%/GameSaveCloudBackup/config.json`
- Local logs at `%LOCALAPPDATA%/GameSaveCloudBackup/Logs/app.log`
- rclone availability check using `rclone version`
- rclone remote listing using `rclone listremotes`
- Remote validation helper
- Remote text file read helper
- Safe remote path builder
- Manual Backup Now using `rclone copy`
- Versioned backup copy using `rclone copy`
- Metadata upload using `rclone copyto`
- Manual Restore from Cloud using `rclone copy`
- Startup cloud metadata check and restore prompt when cloud appears newer
- Session-only prompt tracking so startup prompts are not repeated in the same app session
- Local safety backup before restore at `%LOCALAPPDATA%/GameSaveCloudBackup/SafetyBackups/`

Not implemented yet:

- Game process monitoring
- Automatic backup while a game is running
- Final backup when a game closes

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

## Startup restore prompt

When the app opens, it checks configured games that have `startupRestorePrompt` enabled. For each game, it tries to read:

```text
<remote>:<cloudPath>/metadata.json
```

If metadata exists, the app compares the cloud backup date to the latest modified time in the local save folder. If the cloud backup appears newer, or if the local save folder is missing, the app shows a restore prompt with:

- Cloud backup date
- Source device
- Local save date, when available

Prompt choices:

- **Restore from Cloud** — runs the same safe restore path, including local safety backup first.
- **Keep Local Save** — dismisses the startup prompt for this app session.
- **Ask Later** — also dismisses the automatic startup prompt for this app session. Manual restore remains available.

If rclone is missing, metadata is missing/invalid, or the cloud backup is older than/equal to local saves, the app logs the result and does not prompt. No drama. Rare, but appreciated.

## Manual backup test

1. Create or choose a small local test save folder with at least one file.
2. Configure a game in the app:
   - Game Name: `Stardew Valley` or any test name
   - Save Folder: your test save folder
   - Rclone Remote: `gdrive` or your configured remote
   - Cloud Backup Folder: `GameSaveBackups/Stardew Valley`
3. Click **Backup Now**.
4. Confirm files appear in:

```text
gdrive:GameSaveBackups/Stardew Valley/latest
gdrive:GameSaveBackups/Stardew Valley/versions/<TIMESTAMP>
gdrive:GameSaveBackups/Stardew Valley/metadata.json
```

Equivalent inspection commands:

```bash
rclone lsf "gdrive:GameSaveBackups/Stardew Valley/latest"
rclone lsf "gdrive:GameSaveBackups/Stardew Valley/versions"
rclone cat "gdrive:GameSaveBackups/Stardew Valley/metadata.json"
```

## Manual restore test

1. Make sure a backup exists in `<remote>:<cloudPath>/latest`.
2. Select the configured game in the app.
3. Click **Restore from Cloud**.
4. Review the confirmation dialog showing metadata when available.
5. Confirm restore.
6. The app first creates a local safety backup under:

```text
%LOCALAPPDATA%/GameSaveCloudBackup/SafetyBackups/GameName/before_restore_TIMESTAMP/
```

7. Then it restores cloud latest files into the configured local save folder.

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
- Always create a local safety backup before manual restore.

## Cloud folder structure

```text
GameSaveBackups/
└── Game Name/
    ├── latest/
    ├── versions/
    │   └── TIMESTAMP/
    └── metadata.json
```

Example metadata:

```json
{
  "gameName": "Stardew Valley",
  "lastBackupTime": "2026-04-28T10:30:00+08:00",
  "sourceDevice": "USER-PC",
  "backupType": "manual",
  "savePath": "C:\\Users\\User\\AppData\\Roaming\\StardewValley\\Saves",
  "appVersion": "1.0.0"
}
```

Example remote paths:

```text
gdrive:GameSaveBackups/Stardew Valley/latest
gdrive:GameSaveBackups/Stardew Valley/versions/20260428_103000
gdrive:GameSaveBackups/Stardew Valley/metadata.json
```
