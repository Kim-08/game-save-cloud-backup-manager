# Roadmap

## Phase 0: Project definition

Create repository memory, planning documents, architecture notes, roadmap, risk list, and decision records.

Status: completed.

## Phase 1: Repo and desktop shell

Create the C# desktop application shell using WinForms. Add basic project structure, main window, app settings, placeholder views, config storage, logging, and basic game management.

Status: completed.

## Phase 2: rclone integration

Add rclone discovery/configuration checks, command execution wrapper, remote listing, remote validation, metadata text-read foundation, remote path building, and error handling.

Status: completed.

## Phase 3: Game management

Add UI and storage for creating, editing, deleting, and validating game entries, then expose per-game actions.

Status: completed.

## Phase 4: Backup, restore, and startup restore prompt

Implement manual Backup Now and Restore from Cloud using safe `rclone copy`, versioned backups, metadata upload/read, local safety backup before restore, restore confirmation, startup cloud metadata checks, and one-prompt-per-session restore prompt behavior.

Status: completed.

## Phase 5: Game monitoring and automatic backup

Implement process monitoring, 10-minute default running-game backup interval, and final backup when the game exits.

Status: completed.

## Phase 6: Polish, packaging, and reliability

Improve UX, logs, validation, reliability, packaging, documentation, release workflow, and early-user readiness.

Status: completed.

Completed highlights:

- Better logs viewer and folder buttons.
- Rclone setup help.
- Better Add/Edit validation.
- Friendly no-games empty state.
- Backup history based on cloud `versions/` folder.
- Safe retention for managed timestamped version backups.
- Config corruption rename/recovery to `config.corrupt.TIMESTAMP.json` with a fresh clean config.
- Manual restore and startup restore blocking when the configured game process is running.
- Close-backup cancellation that avoids starting delayed final backups after monitoring stops.
- More defensive rclone execution and cancellation handling.
- Windows publish script.
- GitHub Actions build + publish validation.
- README and documentation cleanup.

## Phase 7: Post-MVP release hardening

Recommended next step.

Potential tasks:

- Add installer or MSIX packaging.
- Add GitHub Releases artifact upload.
- Add optional launcher process-name override for games launched by Steam/Epic/etc.; current docs recommend selecting the actual game executable instead of the launcher where possible.
- Allow restoring from a selected versioned backup, not only `latest/`.
- Add richer conflict detection and comparison UI.
- Add settings screen for global defaults.
- Consider system tray/start-minimized support after the shutdown/cancellation behavior, pending auto-backup cancellation, and close-backup cancellation have more real-world testing.
- Add automated tests around path validation, retention selection, config corruption handling, and rclone argument building.

Status: not started.
