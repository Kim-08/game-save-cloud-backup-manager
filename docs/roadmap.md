# Roadmap

## Phase 0: Project definition

Create repository memory, planning documents, architecture notes, roadmap, risk list, and decision records.

## Phase 1: Repo and desktop shell

Create the C# desktop application shell using WinForms. Add basic project structure, main window, app settings, placeholder views, config storage, logging, and basic game management.

## Phase 2: rclone integration

Add rclone discovery/configuration checks, command execution wrapper, remote listing, remote validation, metadata text-read foundation, remote path building, and error handling.

## Phase 3: Manual backup and restore

Implement manual backup and manual restore using safe `rclone copy`, metadata generation/read/write, backup result logging, and clear restore confirmation. Do not implement automatic game monitoring yet.

## Phase 4: Startup restore prompt and backup metadata behavior

Implement startup metadata check, cloud-newer restore prompt, one-prompt-per-session behavior, and conflict-safe UX around metadata differences.

## Phase 5: Game monitoring and auto backup

Implement process monitoring, 10-minute default running-game backup interval, and final backup when the game exits.

## Phase 6: Polish and packaging

Improve UX, logs, validation, settings, packaging, documentation, and release workflow.
