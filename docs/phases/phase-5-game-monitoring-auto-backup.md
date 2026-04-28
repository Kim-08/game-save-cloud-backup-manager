# Phase 5: Game Monitoring and Auto Backup

## Goal

Automatically back up saves while games are running and after they close.

## Tasks

- Detect running game process.
- Start per-game backup timer while running.
- Default backup interval: 10 minutes.
- Trigger final backup when game closes.
- Avoid overlapping backup jobs.
- Log skipped, failed, and successful backups.

## Exit criteria

- Running games are backed up on interval.
- Closing a game triggers one final backup.
