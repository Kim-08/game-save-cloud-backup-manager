# rclone Research Notes

## Why rclone

rclone supports many cloud providers behind one command-line interface. This avoids building and maintaining direct integrations for Google Drive, Dropbox, OneDrive, and other provider APIs.

The app should not store cloud provider credentials. rclone owns provider authentication and stores its own config separately.

## Provide rclone

Published app builds bundle rclone under `tools/rclone/rclone.exe`. Local development can also use a PATH-installed rclone downloaded from:

<https://rclone.org/downloads/>

Verify installation:

```text
rclone version
```

If the command fails during local development, rclone is either not installed or not available in PATH.

## Configure rclone

Start configuration:

```text
rclone config
```

Create a remote, for example a Google Drive remote named:

```text
gdrive
```

Verify configured remotes:

```text
rclone listremotes
```

Example output:

```text
gdrive:
```

The app stores the remote name as `gdrive` without the trailing colon.

## Relevant commands

### Check rclone availability and version

```text
rclone version
```

### List remotes

```text
rclone listremotes
```

### Validate a remote

```text
rclone lsd "gdrive:"
```

This is a safe validation-style command. Some remotes may fail if auth is expired or root listing is unavailable.

### Read cloud metadata text

```text
rclone cat "gdrive:GameSaveBackups/Stardew Valley/metadata.json"
```

### Copy local saves to cloud

Planned for a later phase:

```text
rclone copy "<local-save-folder>" "<remote>:GameSaveBackups/<Game Name>/latest"
```

### Copy cloud saves to local folder

Planned for a later phase:

```text
rclone copy "<remote>:GameSaveBackups/<Game Name>/latest" "<local-save-folder>"
```

### Upload cloud metadata

Implementation can write metadata to a local temp file, then copy it to the remote destination.

## Remote path examples

Remote name:

```text
gdrive
```

Cloud path:

```text
GameSaveBackups/Stardew Valley
```

Latest remote path:

```text
gdrive:GameSaveBackups/Stardew Valley/latest
```

Metadata path:

```text
gdrive:GameSaveBackups/Stardew Valley/metadata.json
```

## Default safety stance

Use `rclone copy` by default. Avoid `rclone sync` by default because sync can delete files from the destination.

Command execution should:

- Run asynchronously.
- Capture stdout, stderr, and exit code.
- Log command starts and summarized results.
- Avoid logging secrets or credentials.
- Never store rclone credentials in this app.
