# Phase 5: Game Monitoring and Auto Backup

## Goal

Automatically back up saves while games are running and after they close.

## Delivered

- Added `Services/GameMonitorService.cs`.
- Monitors configured games by deriving process name from `exePath`.
- Checks running/not-running state every few seconds.
- Updates UI status for each game:
  - Running / Not Running
  - Auto Backup enabled state
  - Auto backup interval
  - Backup currently running
  - Last auto backup time
- Logs `Game started` when a monitored process appears.
- Waits about one minute before the first automatic backup after game start.
- Runs automatic backup every configured interval while the game remains running.
- Defaults to 10 minutes via game config.
- Logs and skips interval backup if a backup for the same game is already running.
- Logs `Game closed` when a monitored process exits.
- Waits about five seconds after close.
- Runs one final `close` backup if `backupOnClose` is enabled.
- Calls `BackupService.BackupNowAsync(game, "auto")` for running-game backups.
- Calls `BackupService.BackupNowAsync(game, "close")` for final close backups.
- Does not freeze UI; monitoring and backups use background async tasks.
- Add/Edit Game already supports enabling/disabling auto backup per game.

## Safety rules implemented

- Do not back up if save folder is missing.
- Do not back up if save folder is empty.
- Do not run overlapping backups for the same game.
- Log invalid EXE paths.
- Log process lookup failures.
- Log rclone/backup failures.
- Keep rclone credentials outside the app.

## Explicitly not included

- No packaging/release installer yet.
- No advanced retry/backoff policy yet.
- No tray app behavior yet.
- No richer process matching for launchers that spawn child processes yet.

## Status

Completed.

## Next phase

Phase 6: polish, packaging, and reliability.
