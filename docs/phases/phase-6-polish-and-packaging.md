# Phase 6: Polish and Packaging

## Goal

Prepare the app for practical MVP use and early distribution.

## Status

Completed.

## Completed tasks

- Improved validation and error messages in Add/Edit Game.
- Added startup restore prompt toggle and version retention field to Add/Edit Game.
- Added friendly empty state when no games are configured.
- Replaced the simple recent-log list with a scrollable logs viewer.
- Added Refresh Logs, Open Logs Folder, and Open Config Folder buttons.
- Kept Open Save Folder as a per-game action.
- Added rclone setup help in the UI and expanded README setup guidance.
- Added backup history display based on managed timestamped folders under the cloud `versions/` folder.
- Added safe version retention: only timestamp-named managed backup folders are pruned, and `0` keeps all versions.
- Added config corruption handling by backing up invalid config JSON before creating a fresh config.
- Hardened logging so locked/unreadable log files do not crash the app.
- Added defensive rclone command handling, cancellation support, process-tree kill on cancellation, and sensitive argument redaction in logs.
- Improved async UI error handling for backup, restore, rclone refresh, and history operations.
- Ensured background monitoring is canceled/stopped during app close.
- Added `scripts/publish-windows.ps1` for Release Windows publish output.
- Added bundled rclone publishing and in-app rclone configuration using an app-owned config file.
- Updated GitHub Actions to build and validate publish output.
- Updated README, current state, project memory, changelog, roadmap, and risks.

## Explicitly deferred

- Installer/MSIX packaging.
- App icon placeholder.
- Start minimized/system tray support.
- Advanced launcher child-process matching.
- Restoring from a selected historical version.

These are better post-MVP items. Tray behavior especially is deceptively simple until shutdown semantics become cursed.

## Exit criteria

- App is documented well enough for early users.
- App can be published with a simple PowerShell script.
- MVP feature checklist is complete.
- Reliability failure modes are safer and better logged.

Result: met.
