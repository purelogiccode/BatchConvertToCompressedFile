# What's new

## 2.0.0 — cross-platform rebuild

**Released:** 2026-11-06

Batch Convert to Compressed File 2.0.0 is a complete cross-platform rebuild of the 1.3.1 Windows
application. The archive engine is now fully managed, the UI runs on Avalonia, and official
bundles are produced for Windows, Linux, and macOS on x64 and arm64.

### Highlights

- **Windows, Linux, and macOS** support on **x64 and arm64**.
- **Avalonia 12 UI** with a black/indigo theme, a resizable live log on the right, and tooltips on
  every control.
- **SharpCompress archive engine** — no native `7z.dll` / `7z.so` binaries.
- **Batch compression** to individual `.7z` (LZMA2) or `.zip` (Deflate) archives.
- **Batch verification** of `.7z`, `.zip`, and `.rar` archives with full size and CRC-32 checks.
- **Safer file handling** — existing archives are never overwritten; partial archives are removed;
  archive moves are contained inside the selected destination.
- **Portable output names** — Windows-reserved names, trailing dots/spaces, and duplicate base
  names are handled safely.
- **Skipped statistics** in the UI, alongside total, success, failed, elapsed, and write speed.
- **Serilog logging** with a live in-app log and Warning+ forwarding to the bug report API.
- **GitHub update check**, donation verification, and anonymous startup statistics.
- **Framework-dependent single-file bundles** — one executable per platform, no .NET runtime
  embedded. The .NET 10 runtime must be installed on the target machine.

### Fixed since 1.3.1

- Canceled compression no longer leaves a partial archive at the destination.
- Two source files with the same base name no longer silently overwrite or skip each other.
- Verification moves cannot escape the destination folder through `..` paths.
- Update checks treat timeouts as "up to date" instead of raising a startup error.
- Donation response reads now honor the request timeout.
- Expected conditions such as locked files, corrupt archives, and offline networks are logged at
  Information instead of generating noisy bug reports.
- Every `async void` UI event handler is guarded with `try/catch`.
- Donor email addresses are no longer written to logs or persisted in local state.
- Archive verification skips inaccessible directories and reparse points instead of aborting.
- Cross-volume archive moves fall back to copy-then-delete.

### Upgrade notes from 1.3.1

1. Install the **.NET 10 runtime** if it is not already present.
2. Download the bundle for your platform from the latest release.
3. Extract it and run `BatchConvertToCompressedFile` (`.exe` on Windows).
4. Existing `.7z`, `.zip`, and `.rar` archives remain compatible.
5. The 2.0.0 bundles include `LICENSE`, `ReadMe.md`, and this `WhatsNew.md`.

### Downloads

| Platform | x64 | arm64 |
| --- | --- | --- |
| Windows | `BatchConvertToCompressedFile_2.0.0_win-x64.zip` | `BatchConvertToCompressedFile_2.0.0_win-arm64.zip` |
| Linux | `BatchConvertToCompressedFile_2.0.0_linux-x64.tar.gz` | `BatchConvertToCompressedFile_2.0.0_linux-arm64.tar.gz` |
| macOS | `BatchConvertToCompressedFile_2.0.0_osx-x64.tar.gz` | `BatchConvertToCompressedFile_2.0.0_osx-arm64.tar.gz` |

SHA-256 checksums are published as `checksums.txt` on the release page.
