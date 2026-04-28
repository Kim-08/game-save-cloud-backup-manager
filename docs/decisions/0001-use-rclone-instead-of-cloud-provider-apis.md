# 0001: Use rclone instead of cloud provider APIs

## Status

Accepted

## Decision

Use rclone as the cloud storage engine instead of directly integrating Google Drive, Dropbox, OneDrive, or other cloud provider APIs.

## Context

The app needs to support cloud backup destinations without maintaining separate authentication and API implementations for each provider.

## Consequences

- Faster provider support through rclone.
- App can focus on UI, game management, backup orchestration, and safety.
- Users must install/configure rclone or the app must later help provide it.
