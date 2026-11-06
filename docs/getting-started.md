# Getting started

## Requirements

| Platform | Architectures | Notes |
| --- | --- | --- |
| Windows 10/11 | x64, arm64 | Self-contained release bundles include .NET 10 |
| Linux | x64, arm64 | Requires a desktop session (X11 or Wayland) |
| macOS | x64, arm64 | Intel and Apple Silicon |

The application is self-contained in official releases. Building from source requires the
**.NET 10 SDK**.

## Download and install

1. Open <https://github.com/purelogiccode/BatchConvertToCompressedFile/releases>.
2. Download the asset matching your platform:
   - `BatchConvertToCompressedFile_<version>_win-x64.zip`
   - `BatchConvertToCompressedFile_<version>_win-arm64.zip`
   - `BatchConvertToCompressedFile_<version>_linux-x64.tar.gz`
   - `BatchConvertToCompressedFile_<version>_linux-arm64.tar.gz`
   - `BatchConvertToCompressedFile_<version>_osx-x64.tar.gz`
   - `BatchConvertToCompressedFile_<version>_osx-arm64.tar.gz`
3. Extract the archive anywhere you can write.
4. Run `BatchConvertToCompressedFile` (`.exe` on Windows).
5. Optionally verify the SHA-256 checksum published with the release.

On Linux/macOS, make the executable runnable if your extractor removed the bit:

```bash
chmod +x BatchConvertToCompressedFile
```

## First compression

1. Open the **Compress Files** tab.
2. Choose a **Source folder** containing the files to compress.
3. Choose an **Output folder** (it must be different from the source folder).
4. Select `.7z` or `.zip`.
5. Optionally enable **Delete original files after successful compression**.
6. Click **Start compression**.

Each source file becomes its own archive. Existing archives in the output folder are skipped and
never overwritten. The live log is shown on the right, and progress and statistics are shown
below the settings.

## First verification

1. Open the **Verify Archives** tab.
2. Choose the **Archives folder**.
3. Enable **Include subfolders** if needed.
4. Optionally configure success/failed move folders.
5. Click **Start verification**.

Every entry of every supported archive is fully read. Size and CRC checks are performed by the
application, so corrupt archives are reported even when the container still opens.

## Where settings and logs live

| Item | Windows | Linux | macOS |
| --- | --- | --- | --- |
| Logs | `%LOCALAPPDATA%\BatchConvertToCompressedFile\logs` | `~/.local/share/BatchConvertToCompressedFile/logs` | `~/Library/Application Support/BatchConvertToCompressedFile/logs` |
| Donation cache | `...\BatchConvertToCompressedFile\donation.json` | same pattern | same pattern |

Use **File -> Open Log Folder** to reveal the log directory.

## Next steps

- [User guide](user-guide.md)
- [Configuration](configuration.md)
- [Troubleshooting](troubleshooting.md)
