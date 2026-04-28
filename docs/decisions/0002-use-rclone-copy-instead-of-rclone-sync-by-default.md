# 0002: Use rclone copy instead of rclone sync by default

## Status

Accepted

## Decision

Use `rclone copy` by default. Do not use `rclone sync` by default.

## Context

`rclone sync` can delete destination files. That is powerful. Also a loaded crossbow in a sock drawer.

## Consequences

- Safer default behavior.
- Fewer accidental deletions.
- Future sync features, if added, need explicit warnings and safeguards.
