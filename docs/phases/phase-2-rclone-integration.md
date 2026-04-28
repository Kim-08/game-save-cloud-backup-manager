# Phase 2: rclone Integration

## Goal

Add a safe wrapper around rclone command execution and expose rclone availability/remotes in the desktop shell.

## Delivered

- Added `Services/RcloneService.cs`.
- Added async/background rclone command execution so the UI does not freeze.
- Captures stdout, stderr, exit code, and duration.
- Logs rclone command starts and summarized results.
- Redacts common secret-like command arguments before logging.
- Handles missing rclone gracefully with a clear error message.
- Checks rclone availability using:

```text
rclone version
```

- Gets rclone version from `rclone version`.
- Lists configured remotes using:

```text
rclone listremotes
```

- Validates a remote with a safe read/list-style command.
- Reads remote text files using `rclone cat` for future metadata support.
- Builds normalized remote paths such as:

```text
gdrive:GameSaveBackups/Stardew Valley/latest
gdrive:GameSaveBackups/Stardew Valley/metadata.json
```

- Main window now shows:
  - rclone installed or missing
  - rclone version if available
  - number/list of configured remotes if available
- Add/Edit Game window now allows users to select an existing remote or enter one manually.
- Add/Edit Game window includes a Test Remote action.

## Explicitly not included

- No real backup workflow.
- No real restore workflow.
- No automatic game process monitoring.
- No rclone credential storage.
- No cloud provider credential storage.
- No destructive sync behavior.

## Status

Completed.

## Next phase

Phase 3: manual backup and restore.
