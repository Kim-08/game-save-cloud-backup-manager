# Project Memory

## Main app idea

Game Save Cloud Backup Manager is a Windows desktop app that lets users add games, select the game EXE or launcher, select the game save folder, and automatically back up those saves to cloud storage using rclone.

## Confirmed design choices

- Build a Windows desktop app first.
- Initial MVP target is C# with WPF or WinForms.
- Phase 1 selected WinForms for the MVP because it is simpler and maintainable for the first desktop shell.
- Use rclone as the cloud engine.
- Phase 2 added `RcloneService` for rclone detection, version checks, remote listing, remote validation, remote text reads, safe async command execution, and remote path building.
- Do not directly integrate Google Drive, Dropbox, OneDrive, or other cloud provider APIs.
- The app owns UI, game management, local JSON config, rclone command execution, cloud metadata, startup restore prompts, manual backup/restore, game monitoring, auto backup, final backup on close, logs, and error handling.
- Use `rclone copy` by default.
- Do not use `rclone sync` by default.
- Start with safe one-way backup and manual restore.
- Store configuration under `%LOCALAPPDATA%`.
- Current local config path is `%LOCALAPPDATA%/GameSaveCloudBackup/config.json`.
- Current log path is `%LOCALAPPDATA%/GameSaveCloudBackup/Logs/app.log`.
- The app checks rclone on startup with `rclone version` and lists remotes with `rclone listremotes`.
- The app must not store rclone credentials or cloud provider credentials; rclone owns its own auth/config.
- On startup, check cloud backup metadata and ask whether to restore if the cloud save is newer.
- After the startup restore prompt, do not auto-restore again in the same session unless the user manually chooses Restore.
- While a game is running, back up every 10 minutes by default.
- When a game closes, perform one final backup.

## Open questions

- Should rclone be bundled, downloaded, or user-provided? Current Phase 2 assumption: user-provided rclone in PATH.
- How should game process detection handle launchers that spawn another process?
- Should versioned backups be implemented in MVP or after `latest/` works reliably?
- How should conflicts be displayed when local and cloud metadata disagree?
- Should backup intervals be global, per-game, or both?

## Future agent instructions

Before modifying code in future sessions, read:

1. `docs/current-state.md`
2. `docs/roadmap.md`
3. `docs/architecture.md`
4. `docs/project-memory.md`

Then update `docs/current-state.md` and `docs/changelog.md` after meaningful changes.

Phase 1 technology choice is resolved: WinForms. Future agents should keep the app simple unless a strong reason emerges to migrate UI frameworks.

Phase 2 is complete. Next recommended implementation phase is Phase 3: manual backup and restore. Do not implement automatic game monitoring until the later monitoring phase.

## Notes

This repository is intentionally planning-first. It should preserve decisions and context so future sessions do not rediscover the same cursed cave walls by torchlight.
