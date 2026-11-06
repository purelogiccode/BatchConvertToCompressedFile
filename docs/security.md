# Security

See the repository-level [SECURITY.md](https://github.com/purelogiccode/BatchConvertToCompressedFile/blob/master/SECURITY.md)
for the vulnerability disclosure process. This page summarizes how the application is hardened.

## Attack surface

| Area | Exposure |
| --- | --- |
| Archive parsing | Untrusted `.7z`, `.zip`, `.rar` files |
| File paths | User-selected folders and archive entry names |
| Network | GitHub API, bug report, stats, donation endpoints |
| Local state | `donation.json` and rolling log files |
| Updates | Browser redirect only; no auto-download or execution |

## Archive safety

- SharpCompress is managed code; no native library is loaded.
- Verification reads entry streams into memory-bounded buffers and discards them.
- Entry sizes and CRC-32 values are checked.
- A per-archive timeout prevents indefinite hangs.
- The app never extracts entries to disk during verification.
- Archives are created in the chosen output folder only.

## Path handling

- Output archive names are sanitized with the platform's invalid file-name characters.
- Input and output folders must differ.
- Move operations preserve relative subfolders and never overwrite existing files.
- Partial archives are deleted on failure/cancellation.

## Update safety

- The app only reads the GitHub release JSON.
- A user confirmation is required before opening a download URL.
- No binary is downloaded, executed, or replaced by the application.
- Download URLs come from GitHub's API response over HTTPS.

## Local data

- Logs contain operation details and possible file paths.
- Bug reports contain environment details and stack traces.
- The donation cache stores the verified email address.
- Delete the log folder or `donation.json` to remove local data.

## Supply chain

- Dependabot, CodeQL, Dependency Review, and NuGet audit are enabled.
- Workflows use least-privilege permissions.
- Release assets include SHA-256 checksums.

## Reporting

Report vulnerabilities privately through
<https://github.com/purelogiccode/BatchConvertToCompressedFile/security/advisories/new>.
