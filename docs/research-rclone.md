# rclone Research Notes

## Why rclone

rclone supports many cloud providers behind one command-line interface. This avoids building and maintaining direct integrations for Google Drive, Dropbox, OneDrive, and other provider APIs.

## Relevant commands

### List remotes

```text
rclone listremotes
```

### Copy local saves to cloud

```text
rclone copy "<local-save-folder>" "<remote>:GameSaveBackups/<Game Name>/latest"
```

### Copy cloud saves to local folder

```text
rclone copy "<remote>:GameSaveBackups/<Game Name>/latest" "<local-save-folder>"
```

### Inspect cloud metadata

```text
rclone cat "<remote>:GameSaveBackups/<Game Name>/metadata.json"
```

### Upload cloud metadata

Implementation can write metadata to a local temp file, then copy it to the remote destination.

## Default safety stance

Use `rclone copy` by default. Avoid `rclone sync` by default because sync can delete files from the destination.
