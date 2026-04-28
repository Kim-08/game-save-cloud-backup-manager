# Project Memory

## Main app idea

Game Save Cloud Backup Manager is a Windows desktop app that lets users add games, select the game EXE or launcher, select the game save folder, and automatically back up those saves to cloud storage using rclone.

## Confirmed design choices

- Build a Windows desktop app first.
- Initial MVP target is C# with WPF or WinForms.
- Use rclone as the cloud engine.
- Do not directly integrate Google Drive, Dropbox, OneDrive, or other cloud provider APIs.
- The app owns UI, game management, local JSON config, rclone command execution, cloud metadata, startup restore prompts, manual backup/restore, game monitoring, auto backup, final backup on close, logs, and error handling.
- Use `rclone copy` by default.
- Do not use `rclone sync` by default.
- Start with safe one-way backup and manual restore.
- Store configuration under `%LOCALAPPDATA%`.
- On startup, check cloud backup metadata and ask whether to restore if the cloud save is newer.
- After the startup restore prompt, do not auto-restore again in the same session unless the user manually chooses Restore.
- While a game is running, back up every 10 minutes by default.
- When a game closes, perform one final backup.

## Open questions

- Should the first UI be WPF or WinForms?
- Should rclone be bundled, downloaded, or user-provided?
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

Do not start implementation before confirming the intended Phase 1 technology choice if it is still unresolved.

## Notes

This repository is intentionally planning-first. It should preserve decisions and context so future sessions do not rediscover the same cursed cave walls by torchlight.
