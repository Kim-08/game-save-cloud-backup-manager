# Phase 4: Manual Backup and Restore

## Goal

Implement manual backup and manual restore using safe rclone commands.

## Delivered

- Added `BackupService`.
- Added manual Backup Now from the game list UI.
- Added manual Restore from Cloud from the game list UI.
- Added Open Save Folder from the game list UI.
- Added Last Backup display.
- Validates save folder exists before backup.
- Rejects backup when the save folder is empty.
- Validates rclone availability and remote access before operations.
- Uses `rclone copy`, not `rclone sync`.
- Uploads local save folder to:

```text
REMOTE:CloudPath/latest
```

- Also uploads a versioned backup to:

```text
REMOTE:CloudPath/versions/TIMESTAMP
```

- Creates and uploads `metadata.json` to:

```text
REMOTE:CloudPath/metadata.json
```

- Updates local `lastBackupTime` after successful backup.
- Reads cloud metadata before restore when available.
- Shows restore confirmation with cloud backup date and source device when metadata exists.
- Creates a local safety backup before restore at:

```text
%LOCALAPPDATA%/GameSaveCloudBackup/SafetyBackups/GameName/before_restore_TIMESTAMP/
```

- Restores cloud latest to local save folder using:

```text
rclone copy "REMOTE:CloudPath/latest" "LOCAL_SAVE_FOLDER" --create-empty-src-dirs
```

- Logs success and failure cases.
- Leaves a TODO for checking whether the game process is running once process monitoring exists.

## Error handling covered

- rclone missing
- remote missing or unavailable
- invalid save folder
- empty save folder
- failed latest upload
- failed version upload
- failed metadata upload
- failed metadata read
- failed local safety backup
- failed restore

## Explicitly not included

- No startup restore prompt yet.
- No automatic game-running backup yet.
- No final backup on game close yet.
- No process monitoring yet.
- No destructive sync behavior.

## Status

Completed for manual backup and manual restore.

## Next phase

Phase 5: startup restore prompt and game monitoring.
