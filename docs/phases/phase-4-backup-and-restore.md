# Phase 4: Backup and Restore

## Goal

Implement manual backup, manual restore, metadata handling, and startup restore prompt.

## Tasks

- Manual backup using `rclone copy`.
- Metadata generation and upload.
- Manual restore using `rclone copy`.
- Startup metadata check.
- Prompt when cloud backup is newer.
- Ensure startup prompt happens only once per app session unless manually invoked.

## Exit criteria

- Users can safely back up and restore configured games manually.
- Startup restore prompt behavior works as specified.
