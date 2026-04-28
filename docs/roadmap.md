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

Improve UX, logs, validation, retry/backoff behavior, launcher process matching, settings, packaging, documentation, release workflow, and installer/distribution story.

Status: next recommended step.
