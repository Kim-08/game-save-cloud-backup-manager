# Phase 3: Manual Backup and Restore

> Note: this file was originally created as `phase-3-game-management.md`. Basic game management was completed during Phase 1, so Phase 3 now focuses on manual backup and restore.

## Goal

Implement explicit user-triggered backup and restore workflows using safe `rclone copy` commands.

## Tasks

- Add manual Backup action for a selected game.
- Add manual Restore action for a selected game.
- Use `RcloneService.BuildRemotePath()` for `latest/` and `metadata.json` remote paths.
- Use `rclone copy` by default.
- Do not use `rclone sync`.
- Generate and upload `metadata.json` after successful backup.
- Read cloud `metadata.json` before restore where available.
- Confirm before restore because restores may overwrite local save files. Tiny data-loss goblin, very real teeth.
- Log backup and restore starts/results.
- Show useful UI errors for missing save path, missing remote, missing cloud path, rclone failure, or remote failure.

## Explicitly not included

- No automatic game monitoring.
- No interval backup while games are running.
- No final backup on game close.
- No destructive sync behavior.

## Exit criteria

- User can manually back up a configured game to `<remote>:<cloudPath>/latest`.
- User can manually restore a configured game from `<remote>:<cloudPath>/latest` after confirmation.
- Metadata is written/read enough to support later startup restore prompt behavior.
- Build passes.
