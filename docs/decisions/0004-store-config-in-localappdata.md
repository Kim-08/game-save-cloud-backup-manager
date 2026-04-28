# 0004: Store config in LocalAppData

## Status

Accepted

## Decision

Store app configuration in `%LOCALAPPDATA%\GameSaveCloudBackupManager`.

## Context

The app needs machine-local configuration for paths, game entries, settings, session state, and logs.

## Consequences

- Configuration stays local to the Windows user account.
- Paths can include machine-specific save locations.
- Future portable mode would require a separate decision.
