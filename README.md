# Game Save Cloud Backup Manager

Game Save Cloud Backup Manager is a Windows desktop app for backing up game saves to cloud storage through [rclone](https://rclone.org/). It lets users add games, point the app at each game EXE/launcher and save folder, choose an rclone remote and cloud folder, then run manual or automatic backups. No direct cloud credentials are stored by the app; rclone owns that tiny box of snakes.

## MVP status

**MVP complete.**

## MVP checklist

- [x] Add/edit/remove games works
- [x] rclone detection works
- [x] Manual backup works
- [x] Manual restore works
- [x] Startup restore prompt works
- [x] Game monitoring works
- [x] Auto backup every interval works
- [x] Final backup on game close works
- [x] Logs work
- [x] README explains setup and publishing

Completed MVP capabilities:

- C# / .NET 8 / WinForms desktop app
- Add, edit, and remove games
- Better Add/Edit validation for EXE path, save folder, remote name, safe relative cloud folder, intervals, and retention
- Friendly empty state when no games are configured
- Local config at `%LOCALAPPDATA%/GameSaveCloudBackup/config.json`
- Config corruption handling: invalid JSON is renamed to `config.corrupt.TIMESTAMP.json` and a fresh config is created
- More reliable config writes using temporary files and overwrite moves
- Local logs at `%LOCALAPPDATA%/GameSaveCloudBackup/Logs/app.log`
- In-app logs viewer with refresh, Open Logs Folder, and Open Config Folder buttons
- rclone detection using `rclone version`
- rclone remote listing using `rclone listremotes`
- rclone setup help button and README setup instructions
- Remote validation helper
- Remote metadata read helper
- Defensive rclone command wrapper with argument-list execution, logging, secret redaction, and cancellation handling
- Manual Backup Now using `rclone copy`
- Versioned backup copy under `versions/TIMESTAMP/`
- Backup history viewer based on the cloud `versions/` folder
- Retention for managed timestamped version folders: keep latest `MaxVersionBackups`, or `0` to keep all
- Metadata upload using `rclone copyto`
- Manual Restore from Cloud using `rclone copy`
- Manual and startup restore are blocked with "Please close the game before restoring." when the configured game process appears to be running
- Local safety backup before restore at `%LOCALAPPDATA%/GameSaveCloudBackup/SafetyBackups/`
- Startup cloud metadata check and restore prompt when the cloud backup appears newer
- Game process monitoring from the configured EXE/launcher path
- Running / Not Running UI status
- Automatic backup after a game starts, then every configured interval
- Final close backup when the game exits, if enabled
- Per-game overlap protection so only one backup or restore runs at a time
- Background monitor cancellation on app close
- Windows GitHub Actions build and publish validation
- Simple Windows publish script at `scripts/publish-windows.ps1`

Not included in the MVP:

- Installer/MSIX packaging
- Bundled rclone binary
- Advanced launcher child-process matching
- System tray/start-minimized behavior
- Conflict-resolution UI beyond restore prompts and safety backups

## Running locally

Requirements:

- Windows
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [rclone](https://rclone.org/downloads/) installed and available in PATH

Run:

```bash
dotnet run --project src/GameSaveCloudBackup/GameSaveCloudBackup.csproj
```

## Publishing for Windows

From PowerShell on Windows:

```powershell
./scripts/publish-windows.ps1
```

Default output:

```text
artifacts/publish/windows-win-x64/
```

Self-contained publish:

```powershell
./scripts/publish-windows.ps1 -SelfContained
```

Equivalent manual command:

```bash
dotnet publish src/GameSaveCloudBackup/GameSaveCloudBackup.csproj \
  --configuration Release \
  --runtime win-x64 \
  --self-contained false \
  --output artifacts/publish/windows-win-x64 \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:EnableCompressionInSingleFile=true
```

Do not commit generated publish output. `artifacts/`, `publish/`, `bin/`, and `obj/` are ignored.

## rclone setup

This app uses rclone as the cloud engine. It does not manage, store, or sync cloud provider credentials directly.

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

Do **not** put `gdrive:folder` in the remote field. Put only `gdrive` in **Rclone Remote**, then put the folder path in **Cloud Backup Folder**, for example:

```text
GameSaveBackups/Stardew Valley
```

## Automatic backup while a game is running

The app monitors each configured game by deriving the process name from the selected EXE/launcher path. Selecting the actual game executable is more reliable than selecting Steam, Epic, or another launcher executable, because launchers often spawn a separate child game process. The app checks every few seconds and updates the game row with:

- Running / Not Running
- Last auto backup time
- Whether a backup is currently running
- Auto backup interval

When a game starts running, the app logs `Game started`, waits about one minute, then runs an automatic backup if auto backup is enabled. While the game remains running, it backs up every configured interval, defaulting to 10 minutes.

When the game closes, the app logs `Game closed`, waits about five seconds, then runs one final backup if **Backup on Close** is enabled.

Safety rules:

- No backup runs if the save folder is missing.
- No backup runs if the save folder is empty.
- No overlapping backups or restores run for the same game.
- Manual restore and startup restore are blocked if the configured game process is running, with the friendly message: "Please close the game before restoring."
- Automatic backups use `BackupService.BackupNowAsync(game, "auto")`.
- Close backups use `BackupService.BackupNowAsync(game, "close")` and receive the monitor cancellation token.

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

- **Restore from Cloud** — runs the safe restore path, including local safety backup first.
- **Keep Local Save** — dismisses the startup prompt for this app session.
- **Ask Later** — also dismisses the automatic startup prompt for this app session. Manual restore remains available.

If the configured game process is already running when a startup restore would be offered, the app shows "Please close the game before restoring." and skips that startup restore attempt for the session. If rclone is missing, metadata is missing/invalid, or the cloud backup is older than/equal to local saves, the app logs the result and does not prompt.

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

## Manual backup test

1. Create or choose a small local test save folder with at least one file.
2. Configure a game in the app:
   - Game Name: `Stardew Valley` or any test name
   - Game EXE/Launcher: a real executable path
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
3. Click **Restore**.
4. Review the confirmation dialog showing metadata when available.
5. Confirm restore. If the configured game process is running, restore stops and asks you to close the game first.
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
