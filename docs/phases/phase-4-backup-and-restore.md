# Phase 4: Backup, Restore, and Startup Cloud Restore Prompt

## Goal

Implement manual backup, manual restore, metadata handling, and startup cloud restore prompt behavior.

## Delivered

### Manual backup and restore

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

### Startup restore prompt

- On app startup, configured games are loaded from local config.
- Games with `startupRestorePrompt` enabled are checked once per app session.
- The app reads cloud metadata with:

```text
rclone cat "REMOTE:CloudPath/metadata.json"
```

- If metadata exists and is valid, the app compares cloud backup time to the local save folder's latest modified time.
- If the cloud backup appears newer than the local save folder, the app shows `RestorePromptDialog`.
- If the local save folder is missing but cloud metadata exists, the app shows `RestorePromptDialog`.
- If cloud metadata is older than or equal to local save time, the app logs and does not prompt.
- If metadata is missing/invalid, the app logs and does not crash.
- If rclone is missing, the startup cloud prompt is skipped and logged.
- Prompt choices:
  - Restore from Cloud
  - Keep Local Save
  - Ask Later
- After a game is checked/prompted, automatic startup checks do not repeat for that game in the same app session.
- Session prompt state is stored in memory only and is not persisted to config.
- Manual Restore from Cloud remains available anytime.
- Startup restore uses the same safe restore path from `BackupService`, including local safety backup before restore.
- TODO remains for blocking restore when a configured game process is running once process detection exists.

## Error handling covered

- rclone missing
- remote missing or unavailable
- invalid save folder
- empty save folder
- failed latest upload
- failed version upload
- failed metadata upload
- failed metadata read
- invalid metadata
- failed local safety backup
- failed restore

## Explicitly not included

- No automatic game-running backup yet.
- No final backup on game close yet.
- No process monitoring yet.
- No destructive sync behavior.

## Status

Completed.

## Next phase

Phase 5: game monitoring and automatic backup.
