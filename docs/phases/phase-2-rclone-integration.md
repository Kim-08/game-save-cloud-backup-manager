# Phase 2: rclone Integration

## Goal

Add a safe wrapper around rclone command execution.

## Tasks

- Detect or configure rclone executable path.
- Run `rclone listremotes`.
- Run `rclone copy` for backup and restore paths.
- Capture stdout, stderr, exit code, and duration.
- Add metadata read/write helpers.
- Add basic diagnostics UI or log view.

## Exit criteria

- App can verify rclone availability.
- App can run a test copy operation through the adapter.
