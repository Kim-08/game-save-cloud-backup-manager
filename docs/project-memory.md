# Project Memory

## Main app idea

Game Save Cloud Backup Manager is a Windows desktop app that lets users add games, select the game EXE or launcher, select the game save folder, and automatically back up those saves to cloud storage using rclone.

## Confirmed design choices

- Build a Windows desktop app first.
- Initial MVP target is C# with WPF or WinForms.
- Phase 1 selected WinForms for the MVP because it is simpler and maintainable for the first desktop shell.
- Use rclone as the cloud engine.
- Do not directly integrate Google Drive, Dropbox, OneDrive, or other cloud provider APIs.
- The app must not store rclone credentials or cloud provider credentials; rclone owns auth/config.
- Use `rclone copy` by default.
- Do not use `rclone sync` by default.
- Start with safe one-way backup and manual restore.
- Store configuration under `%LOCALAPPDATA%`.
- Current local config path is `%LOCALAPPDATA%/GameSaveCloudBackup/config.json`.
- Current log path is `%LOCALAPPDATA%/GameSaveCloudBackup/Logs/app.log`.
- Local safety backups before restore are stored under `%LOCALAPPDATA%/GameSaveCloudBackup/SafetyBackups/`.
- On startup, check cloud backup metadata and ask whether to restore if the cloud save is newer.
- After the startup restore prompt, do not auto-restore again in the same session unless the user manually chooses Restore.
- While a game is running, back up every configured interval, defaulting to 10 minutes.
- When a game closes, perform one final backup when enabled.

## Implemented phases

- Phase 1 added the WinForms shell, config service, logging service, game model, and basic game management.
- Phase 2 added `RcloneService` for rclone detection, version checks, remote listing, remote validation, remote text reads, safe async command execution, and remote path building.
- Phase 3 completed Add/Edit/Remove game management and exposed per-game actions.
- Phase 4 added `BackupService`. Backup uses `rclone copy` to both `latest/` and `versions/<TIMESTAMP>/`, then uploads `metadata.json` with `rclone copyto`. Restore reads metadata where available, requires UI confirmation, creates a local safety backup, then restores cloud `latest/` with `rclone copy`.
- Phase 4 also implemented startup cloud restore prompts. Games with `startupRestorePrompt` enabled are checked once per app session by reading cloud `metadata.json`; if cloud metadata is newer than local save modified time, or local saves are missing, the app prompts Restore from Cloud / Keep Local Save / Ask Later.
- Phase 5 added `GameMonitorService` for process monitoring and automatic backups. It derives the process name from `exePath`, checks every few seconds, updates runtime UI status, waits about one minute after game start before first auto backup, backs up every configured interval, and runs a final close backup after about five seconds when `backupOnClose` is enabled. It prevents overlapping backups per game.
- Phase 6 added polish/reliability/packaging: improved logs viewer, folder-opening buttons, rclone setup help, better Add/Edit validation, friendly empty state, backup history from cloud `versions/`, safe managed-version retention, config corruption recovery, better logging resilience, rclone cancellation handling, Windows publish script, CI publish validation, and documentation cleanup.
- The MVP stabilization pass changed rclone execution from shell-style quoted command strings to `ProcessStartInfo.ArgumentList`, added safe relative cloud-path validation, blocked restore when the configured game process appears to be running, tail-limited log reads, and made config writes use temporary files plus overwrite moves.
- The Phase 6 reliability pass renamed invalid `config.json` files to `config.corrupt.TIMESTAMP.json`, creates a clean replacement config, shows "Please close the game before restoring." when restore is blocked by a running configured process, and makes shutdown cancel pending auto-backup/close-backup delays without starting a final close backup after monitoring stops.

## MVP completion status

MVP is complete for early users if rclone is user-installed and configured.

Completion checklist:

- Add/edit/remove games works.
- rclone detection works.
- Manual backup works.
- Manual restore works.
- Startup restore prompt works.
- Game monitoring works.
- Auto backup every interval works.
- Final backup on game close works.
- Logs work.
- README explains setup and publishing.
- Stabilization build and publish validation pass in the .NET 8 SDK container.

## Open questions / next work

- Should rclone be bundled, downloaded, or remain user-provided? Current assumption: user-provided rclone in PATH.
- How should game process detection handle launchers that spawn another process? Current guidance says to select the actual game executable when possible; likely next step is an optional process-name override.
- Should restore support choosing an older versioned backup, not only `latest/`?
- How should conflicts be displayed when local and cloud metadata disagree?
- Should version retention have a dedicated global setting in addition to per-game `MaxVersionBackups`?
- What distribution format is best: zip, MSIX, installer, winget, or GitHub Releases artifact?

## Future agent instructions

Before modifying code in future sessions, read:

1. `README.md`
2. `docs/current-state.md`
3. `docs/roadmap.md`
4. `docs/architecture.md`
5. `docs/project-memory.md`

Then update `docs/current-state.md` and `docs/changelog.md` after meaningful changes.

Keep restore and auto-backup behavior conservative; accidental save overwrites are not a charming genre.

## Notes

This repository is intentionally planning-first. It should preserve decisions and context so future sessions do not rediscover the same cursed cave walls by torchlight.
