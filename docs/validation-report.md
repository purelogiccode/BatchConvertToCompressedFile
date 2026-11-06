# Validation report

**Date:** 2026-11-05
**Version:** 2.0.0

## Build and tests

| Check | Result |
| --- | --- |
| `dotnet build -c Release -warnaserror` (Windows) | Passed, 0 warnings, 0 errors |
| `dotnet test -c Release` (Windows) | 91 passed, 0 failed |
| `dotnet test -c Release` (WSL Ubuntu 22.04, x64) | 91 passed, 0 failed |
| `dotnet list package --vulnerable --include-transitive` | No vulnerable packages reported |

## Publishing

All six RIDs were published with `--self-contained`, single-file, embedded symbols, from both
Windows and Linux:

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
| Product page | `https://www.purelogiccode.com/Product/batch-convert-to-compressed-file` returns 200 |
| Donation page | `https://www.purelogiccode.com/Donate?app=batch-convert-to-compressed-file` returns 200 |

The website catalog entry was deployed to the production VPS on 2026-11-05. The deployed
`PureLogicCode.dll` was backed up to
`C:\Backups\purelogiccode-donation-20261105-220231` on the server.

## Outstanding items

1. Run the macOS and ARM64 runtime checks when hardware is available.
2. Complete a real donation end-to-end test (the non-donor path and API contract are verified).
