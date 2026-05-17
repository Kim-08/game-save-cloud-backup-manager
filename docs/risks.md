# Risks

## Data loss during restore

Restore can overwrite local saves. Mitigation: require explicit confirmation, create a local safety backup before restore, log restore actions, and avoid automatic restore.

## Misuse of `rclone sync`

`sync` can delete destination files. Mitigation: default to `rclone copy`; do not expose sync until the app has strong warnings and safeguards.

## Incorrect save folder selection

Users may select the wrong folder. Mitigation: validate existence in Add/Edit Game, show paths clearly, provide Open Save Folder, and reject empty save folders during backup.

## Launcher process mismatch

Some launchers start a different game process. Current monitoring derives the process name from the configured EXE/launcher. Mitigation: add optional process-name override in a future phase.

## Cloud metadata drift

Metadata may become stale or fail to upload. Mitigation: treat metadata as advisory, log failures, and avoid destructive automatic behavior.

## rclone availability

rclone may not be installed or configured. Mitigation: provide setup guidance, clear diagnostics, remote testing, and README instructions.

## Long path / locked files

Some saves may be locked while games are running. Mitigation: log errors, use `rclone copy`, allow retries on the next interval, and run a final backup after close.

## Config corruption

Local JSON config may become invalid due to partial writes, manual edits, or crashes. Mitigation: on JSON parse failure, back up the bad config as `config.bad.TIMESTAMP.json`, create a fresh config, and log the recovery.

## Log file locking

External tools or editors may lock the log file. Mitigation: log reads/writes use file sharing where possible and failures are swallowed so diagnostics never crash the app.

## Version retention deleting the wrong remote folder

Retention deletes old cloud version folders. Mitigation: only prune folders whose names match the app-managed timestamp pattern `yyyyMMdd_HHmmss`; `MaxVersionBackups = 0` keeps all versions.

## Cancellation during rclone operations

The app may close while rclone is running. Mitigation: cancellation is propagated through backup/restore paths and the rclone wrapper attempts to kill the rclone process tree.

## Packaging assumptions

The publish script creates app binaries but not an installer. It bundles rclone into `tools/rclone/rclone.exe` by default and falls back to PATH only when the bundled executable is missing. Mitigation: README documents bundled rclone, app-owned rclone config, and the `-SkipRcloneBundle` escape hatch.
