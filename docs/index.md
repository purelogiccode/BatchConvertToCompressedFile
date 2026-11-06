# Batch Convert to Compressed File — Documentation

[![CI](https://github.com/purelogiccode/BatchConvertToCompressedFile/actions/workflows/ci.yml/badge.svg)](https://github.com/purelogiccode/BatchConvertToCompressedFile/actions/workflows/ci.yml)
[![CodeQL](https://github.com/purelogiccode/BatchConvertToCompressedFile/actions/workflows/codeql.yml/badge.svg)](https://github.com/purelogiccode/BatchConvertToCompressedFile/actions/workflows/codeql.yml)
[![Documentation](https://github.com/purelogiccode/BatchConvertToCompressedFile/actions/workflows/docs.yml/badge.svg)](https://purelogiccode.github.io/BatchConvertToCompressedFile/)
[![Latest release](https://img.shields.io/github/v/release/purelogiccode/BatchConvertToCompressedFile?display_name=tag&sort=semver)](https://github.com/purelogiccode/BatchConvertToCompressedFile/releases/latest)
[![License: MIT](https://img.shields.io/github/license/purelogiccode/BatchConvertToCompressedFile)](https://github.com/purelogiccode/BatchConvertToCompressedFile/blob/master/LICENSE)

A cross-platform desktop utility for **batch compressing files** into individual `.7z` or `.zip`
archives and **verifying the integrity** of existing `.7z`, `.zip`, and `.rar` archives.

Built with Avalonia 12, .NET 10, and SharpCompress. Runs on Windows, Linux, and macOS on both
`x64` and `arm64`.

## Start here

| Page | What it covers |
| --- | --- |
| [What's new](whats-new.md) | 2.0.0 release highlights and upgrade notes |
| [Getting started](getting-started.md) | Install, run, and first batch |
| [User guide](user-guide.md) | Compression, verification, progress, statistics, updates |
| [Configuration](configuration.md) | Paths, environment variables, API endpoints |
| [Logging and bug reports](logging-and-bug-reports.md) | Serilog, log files, report format |
| [Donation](donation.md) | Banner behavior, verification API, privacy |
| [Statistics](statistics.md) | Startup usage statistics |
| [Architecture](architecture.md) | Projects, services, models, interfaces, threading |
| [Building and testing](building-and-testing.md) | Local build, tests, publish, static analysis |
| [CI/CD](ci-cd.md) | GitHub Actions, release assets, Pages, and Wiki |
| [Release process](release-process.md) | Tagging, packaging, update assets |
| [Security](security.md) | Threat model, hardening, disclosure |
| [Manual testing](manual-testing.md) | Windows, Linux VM, and macOS UI checklist |
| [Validation report](validation-report.md) | Results of the latest build, test, and smoke runs |
| [Troubleshooting](troubleshooting.md) | Common problems and fixes |
| [FAQ](faq.md) | Short answers |
| [Contributing](contributing.md) | Development conventions |

## Features

- Batch compression to `.7z` (LZMA2) or `.zip` (Deflate)
- Batch verification of `.7z`, `.zip`, and `.rar` with full CRC checking
- Optional deletion of originals after successful compression
- Optional move of verified/failed archives with subfolder preservation
- Resizable live log on the right, plus progress, elapsed time, write speed, and
  total/success/failed/skipped statistics
- Cross-platform Avalonia UI with a black/indigo theme and tooltips on every control
- About window, GitHub update check, donation banner, and application statistics
- Automatic Serilog logging with Warning+ forwarding to the bug report API
- Fully managed archive engine — no native 7-Zip binaries

## Quick command reference

```powershell
dotnet build CSharp_BatchConvertToCompressedFile.sln -c Release
dotnet test  CSharp_BatchConvertToCompressedFile.sln -c Release
dotnet run   --project BatchConvertToCompressedFile
```

## Support

- Website: <https://www.purelogiccode.com>
- Repository: <https://github.com/purelogiccode/BatchConvertToCompressedFile>
- Wiki: <https://github.com/purelogiccode/BatchConvertToCompressedFile/wiki>
- Donate: <https://www.purelogiccode.com/Donate?app=batch-convert-to-compressed-file>
