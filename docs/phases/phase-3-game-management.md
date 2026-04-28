# Phase 3: Game Management

## Goal

Allow users to manage games and their backup configuration.

## Delivered

Basic game management was completed earlier as part of the desktop shell foundation and has now been extended with manual operation buttons.

- Game list UI exists.
- Create/edit/delete game flows exist.
- Game entries are stored in local JSON config.
- Add/Edit Game supports EXE path, save folder path, rclone remote, cloud folder, backup interval, and backup-on-close settings.
- Game rows now show last backup date.
- Game rows now include:
  - Backup Now
  - Restore from Cloud
  - Open Save Folder

## Status

Completed.

## Notes

Manual backup and restore behavior is documented in `phase-4-backup-and-restore.md` because that is where the rclone copy behavior, metadata, safety backup, and restore confirmation live.
