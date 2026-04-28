# 0003: Start with safe one-way backup and manual restore

## Status

Accepted

## Decision

Start with one-way backups to cloud and explicit manual restores, with a startup prompt only when cloud metadata appears newer.

## Context

Automatic bidirectional sync and automatic restore can cause save conflicts or overwrite useful local progress.

## Consequences

- Safer MVP.
- User remains in control of restore operations.
- Conflict resolution can be designed later instead of improvised badly.
