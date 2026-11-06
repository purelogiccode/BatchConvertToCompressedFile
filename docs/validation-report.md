# Validation report

**Date:** 2026-11-06
**Version:** 2.0.0

## Build and tests

| Check | Result |
| --- | --- |
| `dotnet build -c Release` (Windows) | Passed, 0 warnings, 0 errors |
| `dotnet test -c Release` (Windows) | 120 passed, 0 failed |
| `dotnet test -c Debug` (Windows) | 120 passed, 0 failed |
| `dotnet list package --vulnerable --include-transitive` | No vulnerable packages reported |

## Static analysis

| Check | Result |
| --- | --- |
| `jb inspectcode --severity=WARNING --swea` | 0 warnings, 0 errors |
| `jb inspectcode --severity=SUGGESTION --swea` | 0 async-void notes; 5 optional design-hint notes |
| `roslynator analyze --severity-level warning` | 0 diagnostics |

## Publishing

All six RIDs are published as **framework-dependent single-file** builds with embedded symbols.
The .NET runtime is intentionally not embedded in the executable.

- `win-x64`
- `win-arm64`
- `linux-x64`
- `linux-arm64`
- `osx-x64`
- `osx-arm64`

## Desktop smoke tests

| Platform | Result |
| --- | --- |
| Windows 10/11 x64 | App launched; update check ran; stats call executed; log file written |
| Windows 10/11 x64 (regression) | Compressed 2 files to ZIP, then repeated the batch and confirmed 2 skipped; verified 1 valid and 1 corrupt archive (1 success, 1 failure) |
| Linux Mint 22.3 VM (Hyper-V, x64) | App launched at 1080x780; log on the right; About window opened; **2 files compressed to ZIP and both verified successfully** |
| Linux x64 (WSL + Xvfb) | Window, log layout, statistics, and About window verified |
| Linux arm64 | Build/publish validated; runtime validation pending ARM hardware |
| macOS x64/arm64 | Build/publish validated; runtime validation pending macOS hardware |
| Windows arm64 | Build/publish validated; runtime validation pending ARM hardware |

### Linux VM test details

- VM: `LinuxMint` Hyper-V, Linux Mint 22.3 (Zena), kernel 7.0.0-34, x64.
- Published `linux-x64` binary copied to `/home/vm/bcctf`.
- Input: `alpha.txt` and `beta.txt`.
- Output: `alpha.zip` and `beta.zip`.
- Verification: 2 archives checked, 2 succeeded, 0 failed.
- Log path: `/home/vm/.local/share/BatchConvertToCompressedFile/logs/`.

## Live service checks

| Service | Result |
| --- | --- |
| GitHub update check | Latest release read successfully |
| Application statistics | Endpoint reached; `429` returned during testing because of the per-IP rate limit |
| Donation verification (`batch-convert-to-compressed-file`) | **200** with `donated:false` for a non-donor email |
| Donation banner UI (Windows) | Non-donor email verified live; banner stayed visible; log recorded `Donation verification did not find a donation` |
| Product page | `https://www.purelogiccode.com/Product/batch-convert-to-compressed-file` returns 200 |
| Donation page | `https://www.purelogiccode.com/Donate?app=batch-convert-to-compressed-file` returns 200 |

The website catalog entry was deployed to the production VPS on 2026-11-05. The deployed
`PureLogicCode.dll` was backed up to
`C:\Backups\purelogiccode-donation-20261105-220231` on the server.

## Release bundles

All six bundles were produced in `BatchConvertToCompressedFile\bin\Release` without deleting
existing files:

| Bundle | Size |
| --- | --- |
| `BatchConvertToCompressedFile_2.0.0_win-x64.zip` | 13.7 MB |
| `BatchConvertToCompressedFile_2.0.0_win-arm64.zip` | 13.1 MB |
| `BatchConvertToCompressedFile_2.0.0_linux-x64.tar.gz` | 11.6 MB |
| `BatchConvertToCompressedFile_2.0.0_linux-arm64.tar.gz` | 11.3 MB |
| `BatchConvertToCompressedFile_2.0.0_osx-x64.tar.gz` | 14.7 MB |
| `BatchConvertToCompressedFile_2.0.0_osx-arm64.tar.gz` | 14.7 MB |

Each bundle contains exactly one application executable plus `LICENSE`, `ReadMe.md`, and
`WhatsNew.md`. The builds are framework-dependent single-file; the .NET runtime is not embedded.
Linux/macOS tar entries use `0755` for the executable and `0644` for documents. The Windows x64
bundle was extracted and launched successfully. `file` reports a Linux x86-64 ELF executable and
a macOS arm64 Mach-O executable.

Checksums are in `checksums_2.0.0.txt` next to the bundles.

## Documentation deployment

- GitHub Pages: <https://purelogiccode.github.io/BatchConvertToCompressedFile/>
- GitHub Wiki: <https://github.com/purelogiccode/BatchConvertToCompressedFile/wiki>

The Documentation workflow builds MkDocs, deploys Pages, generates the wiki payload, and pushes
the same pages plus `_Sidebar.md` and `_Footer.md` to the wiki.

## Outstanding items

1. Run the macOS and ARM64 runtime checks when hardware is available.
2. Re-run the Linux automated suite and UI smoke tests on the current revision when the Linux VM or
   WSL SDK is available.
3. Complete a real donation end-to-end test (the non-donor path and API contract are verified).
