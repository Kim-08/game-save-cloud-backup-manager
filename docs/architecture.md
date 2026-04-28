# Architecture

## Initial technology target

- Platform: Windows desktop
- Language/runtime: C# / .NET
- UI: WPF or WinForms
- Cloud engine: rclone CLI
- Local persistence: JSON files under `%LOCALAPPDATA%`

## High-level modules

### UI layer

Provides screens for:

- Game list
- Add/edit game
- rclone remote selection
- Manual backup and restore
- Backup status and logs
- Startup restore prompt

### Game management

Stores and validates game entries:

- Game name
- EXE or launcher path
- Save folder path
- rclone remote
- Cloud folder
- Backup interval
- Enabled/disabled status

### Configuration storage

Stores local app configuration as JSON in `%LOCALAPPDATA%\GameSaveCloudBackupManager`.

Likely files:

- `config.json`
- `games.json`
- `session-state.json`
- `logs/`

### rclone adapter

Responsible for:

- Finding or configuring the rclone executable
- Running `rclone copy`
- Listing remotes/folders where needed
- Reading and writing cloud `metadata.json`
- Capturing stdout, stderr, exit code, and duration

### Backup service

Responsible for:

- Manual backup
- Manual restore
- Startup cloud metadata check
- Auto backup scheduling while a game is running
- Final backup on game close

### Process monitor

Responsible for detecting whether a configured game process is running based on the selected EXE or launcher.

## Cloud folder structure

```text
GameSaveBackups/
└── Game Name/
    ├── latest/
    ├── versions/
    └── metadata.json
```

## Backup behavior

Default backup command should use `rclone copy`, not `rclone sync`.

Conceptual command:

```text
rclone copy "<local-save-folder>" "<remote>:GameSaveBackups/<Game Name>/latest"
```

After a successful copy, upload/update `metadata.json`.

## Restore behavior

Restore should be explicit and user-confirmed.

Conceptual command:

```text
rclone copy "<remote>:GameSaveBackups/<Game Name>/latest" "<local-save-folder>"
```

## Safety principles

- Prefer one-way copy over destructive sync.
- Never auto-restore repeatedly in one session.
- Show clear confirmation for restore.
- Keep logs for failed rclone commands.
- Treat cloud metadata as helpful but not infallible. Because metadata lies sometimes. Politely, but it lies.
