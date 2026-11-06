# Batch Convert to Compressed File

[![CI](https://github.com/purelogiccode/BatchConvertToCompressedFile/actions/workflows/ci.yml/badge.svg)](https://github.com/purelogiccode/BatchConvertToCompressedFile/actions/workflows/ci.yml)
[![CodeQL](https://github.com/purelogiccode/BatchConvertToCompressedFile/actions/workflows/codeql.yml/badge.svg)](https://github.com/purelogiccode/BatchConvertToCompressedFile/actions/workflows/codeql.yml)
[![Documentation](https://github.com/purelogiccode/BatchConvertToCompressedFile/actions/workflows/docs.yml/badge.svg)](https://purelogiccode.github.io/BatchConvertToCompressedFile/)
[![Latest release](https://img.shields.io/github/v/release/purelogiccode/BatchConvertToCompressedFile?display_name=tag&sort=semver)](https://github.com/purelogiccode/BatchConvertToCompressedFile/releases/latest)
[![Release date](https://img.shields.io/github/release-date/purelogiccode/BatchConvertToCompressedFile?label=released)](https://github.com/purelogiccode/BatchConvertToCompressedFile/releases/latest)
[![Downloads](https://img.shields.io/github/downloads/purelogiccode/BatchConvertToCompressedFile/total?label=downloads)](https://github.com/purelogiccode/BatchConvertToCompressedFile/releases)
[![Last commit](https://img.shields.io/github/last-commit/purelogiccode/BatchConvertToCompressedFile)](https://github.com/purelogiccode/BatchConvertToCompressedFile/commits/master)
[![Stars](https://img.shields.io/github/stars/purelogiccode/BatchConvertToCompressedFile?label=stars)](https://github.com/purelogiccode/BatchConvertToCompressedFile/stargazers)
[![License: GPLv3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE)
[![Platforms](https://img.shields.io/badge/platforms-Windows%20%7C%20Linux%20%7C%20macOS-6366F1)](https://github.com/purelogiccode/BatchConvertToCompressedFile/releases/latest)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![Avalonia 12](https://img.shields.io/badge/Avalonia-12-8B44F7)](https://avaloniaui.net/)

A cross-platform desktop utility for **batch compressing files** into individual `.7z` or `.zip`
archives and **verifying the integrity** of existing `.7z`, `.zip`, and `.rar` archives.

Built with **Avalonia 12**, **.NET 10**, and **SharpCompress**. Runs on Windows, Linux, and macOS
on both **x64** and **arm64**.

![Batch Convert to Compressed File](screenshot1.png)

## Features

- **Batch compression** — one archive per file, `.7z` (LZMA2) or `.zip` (Deflate)
- **Archive verification** — `.7z`, `.zip`, and `.rar`, with full size and CRC-32 checks
- **Flexible file management** — optionally delete originals after compression, or move verified
  and failed archives into separate folders while preserving subfolders
- **Live feedback** — progress bar, current file, elapsed time, write speed, and total/success/
  failed/skipped statistics
- **Live log on the right** — resizable activity log powered by Serilog
- **Professional black/indigo theme** — tooltips on every control
- **About window** — version, platform, runtime, and project links
- **GitHub update check** — offers to open the download page when a newer release exists
- **Donation banner** — disappears permanently only after a donation is verified by the API
- **Startup statistics** — anonymous application ID and version sent to the stats endpoint
- **Automatic bug reports** — every Warning+ Serilog event is forwarded to the bug report API
- **Fully managed archive engine** — no native 7-Zip binaries on any platform

## Downloads

Pick the asset for your platform from the
[latest release](https://github.com/purelogiccode/BatchConvertToCompressedFile/releases/latest):

| Platform | x64 | arm64 |
| --- | --- | --- |
| Windows | `BatchConvertToCompressedFile_<version>_win-x64.zip` | `BatchConvertToCompressedFile_<version>_win-arm64.zip` |
| Linux | `BatchConvertToCompressedFile_<version>_linux-x64.tar.gz` | `BatchConvertToCompressedFile_<version>_linux-arm64.tar.gz` |
| macOS | `BatchConvertToCompressedFile_<version>_osx-x64.tar.gz` | `BatchConvertToCompressedFile_<version>_osx-arm64.tar.gz` |

Each bundle contains the application, `LICENSE`, `ReadMe.md`, and `WhatsNew.md`. The release also
publishes `checksums.txt` with SHA-256 hashes.

## Requirements

- **Releases:** framework-dependent single-file build — install the **.NET 10 runtime** for your
  platform. The .NET runtime is intentionally **not** embedded in the executable.
- **From source:** .NET 10 SDK
- **Linux/macOS:** a graphical desktop session (X11, Wayland, or Aqua)

## Usage

### Compress files

1. Open the **Compress Files** tab.
2. Select a source folder and an output folder (they must be different).
3. Choose `.7z` or `.zip`.
4. Optionally enable deletion of originals after successful compression.
5. Click **Start compression**.

Existing archives are skipped and never overwritten. Files with duplicate base names are written
as `name.zip`, `name (2).zip`, and so on.

### Verify archives

1. Open the **Verify Archives** tab.
2. Select the archives folder.
3. Optionally enable subfolder scanning and success/failed move folders.
4. Click **Start verification**.

Every entry is fully decompressed and checked against its stored size and CRC-32.

## Build and test

```powershell
dotnet restore CSharp_BatchConvertToCompressedFile.sln
dotnet build   CSharp_BatchConvertToCompressedFile.sln -c Release
dotnet test    CSharp_BatchConvertToCompressedFile.sln -c Release
```

Run:

```powershell
dotnet run --project BatchConvertToCompressedFile
```

Publish a framework-dependent single-file bundle for any RID:

```powershell
dotnet publish BatchConvertToCompressedFile -c Release -r linux-arm64 --self-contained false `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -p:DebugType=embedded `
  -o artifacts/linux-arm64
```

Supported RIDs: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`.

## Documentation

- [Documentation site](https://purelogiccode.github.io/BatchConvertToCompressedFile/)
- [GitHub wiki](https://github.com/purelogiccode/BatchConvertToCompressedFile/wiki)
- [What's new](WhatsNew.md)
- [Getting started](docs/getting-started.md)
- [User guide](docs/user-guide.md)
- [Configuration](docs/configuration.md)
- [Logging and bug reports](docs/logging-and-bug-reports.md)
- [Donation](docs/donation.md)
- [Statistics](docs/statistics.md)
- [Architecture](docs/architecture.md)
- [Building and testing](docs/building-and-testing.md)
- [CI/CD](docs/ci-cd.md)
- [Release process](docs/release-process.md)
- [Security](docs/security.md)
- [Manual testing](docs/manual-testing.md)
- [Validation report](docs/validation-report.md)
- [Troubleshooting](docs/troubleshooting.md)
- [FAQ](docs/faq.md)

## CI/CD

- Build + tests on Windows, Linux, and macOS
- Cross-publish validation for all six RIDs
- Tag-driven releases with ZIP/TAR.GZ packages and SHA-256 checksums
- CodeQL, Dependency Review, Dependabot, and NuGet audit
- MkDocs Material documentation deployed to GitHub Pages and GitHub Wiki

## Security

Please report vulnerabilities privately through
[GitHub Security Advisories](https://github.com/purelogiccode/BatchConvertToCompressedFile/security/advisories/new).
See [`SECURITY.md`](SECURITY.md) for the full policy.

## License

GNU General Public License v3.0 — see [`LICENSE`](LICENSE).

## Support this project

If this tool saves you time, please consider a donation:

**[Donate](https://www.purelogiccode.com/Donate?app=batch-convert-to-compressed-file)**

Every donation helps keep the application free and actively maintained.

## Acknowledgements

- Uses [SharpCompress](https://github.com/adamhathcock/sharpcompress) for managed archive handling.
- Built with [Avalonia](https://avaloniaui.net/) and [Serilog](https://serilog.net/).
- Developed by [Pure Logic Code](https://www.purelogiccode.com).

---

Thank you for using **Batch Convert to Compressed File**!
