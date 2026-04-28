# Risks

## Data loss during restore

Restore can overwrite local saves. Mitigation: require explicit confirmation, consider pre-restore local backup, and keep restore logs.

## Misuse of `rclone sync`

`sync` can delete destination files. Mitigation: default to `rclone copy`; do not expose sync until the app has strong warnings and safeguards.

## Incorrect save folder selection

Users may select the wrong folder. Mitigation: validate existence, show path clearly, and provide test backup.

## Launcher process mismatch

Some launchers start a different game process. Mitigation: allow manual process override later.

## Cloud metadata drift

Metadata may become stale or fail to upload. Mitigation: treat metadata as advisory, log failures, and avoid destructive automatic behavior.

## rclone availability

rclone may not be installed or configured. Mitigation: provide setup guidance and clear diagnostics.

## Long path / locked files

Some saves may be locked while games are running. Mitigation: log errors, retry later, and run final backup after close.
