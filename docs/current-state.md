# Current State

- Phase 2 is completed.
- Repository contains a runnable C# WinForms desktop application shell under `src/GameSaveCloudBackup`.
- The app includes the main window, add/edit game form, local JSON config foundation, logging foundation, and rclone integration foundation.
- Config is stored at `%LOCALAPPDATA%/GameSaveCloudBackup/config.json`.
- Logs are stored at `%LOCALAPPDATA%/GameSaveCloudBackup/Logs/app.log`.
- Game management supports add, edit, remove, config save/load, and list refresh.
- Rclone availability is checked on startup using `rclone version`.
- Main UI shows whether rclone is installed or missing, the rclone version when available, and configured remotes from `rclone listremotes`.
- Add/Edit Game UI allows selecting or entering an rclone remote and testing the remote.
- `RcloneService` supports safe async command execution, remote listing, remote validation, remote text file reading, and remote path building.
- The app does not store rclone credentials or cloud provider credentials.
- No real backup or restore workflow exists yet.
- No automatic game process monitoring exists yet.
- GitHub Actions build workflow exists for Windows.
- Next recommended step: Phase 3, manual backup and restore.
