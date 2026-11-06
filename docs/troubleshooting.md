# Troubleshooting

## The app does not start

- **Windows:** confirm the extracted folder is writable and the ZIP was fully extracted.
- **Linux:** run from a desktop session, not a headless SSH session. Check that `DISPLAY` or
  `WAYLAND_DISPLAY` is set.
- **macOS:** if Gatekeeper blocks the build, allow it in **System Settings -> Privacy & Security**
  or run `xattr -dr com.apple.quarantine ./BatchConvertToCompressedFile`.

Check the log folder:

```
%LOCALAPPDATA%\BatchConvertToCompressedFile\logs
~/.local/share/BatchConvertToCompressedFile/logs
```

## Compression says a file failed

Common causes:

- The file is locked by another program.
- The process lacks read permission on the source file.
- The output folder is read-only or full.
- The source file disappeared after the batch started.

Failed files are counted and logged; the batch continues with the next file.

## An archive is skipped

An archive with the same output name already exists. The application never overwrites existing
archives. Remove or rename the existing archive and run the batch again.

## Verification reports a valid archive as failed

- The archive is corrupt.
- The archive is encrypted and no password is supported by the verifier.
- The archive uses a format/feature SharpCompress cannot decode.
- The file is locked or unreadable.
- The operation exceeded the 10-minute per-archive timeout.

## Move operation did not move a file

- The destination file already exists.
- The destination folder is read-only.
- The file stayed locked after five retries (300 ms apart).
- Success/failed folders were configured to the same folder or to the input folder (validation
  rejects this before the batch starts).

## The update check says "up to date" but a release exists

- The release must be published (not a draft/prerelease) and have a higher version tag.
- Asset names must end with `_<rid>.zip` or `_<rid>.tar.gz`.
- GitHub rate limits or offline networks are treated as "up to date" for automatic checks. Use
  **Help -> Check for Updates** to see the manual result.

## The donation banner will not disappear

The banner is hidden only after the donation API returns `donated: true`.

- Use the exact email address used for the donation.
- The application must be registered as donationware on the website with slug
  `batch-convert-to-compressed-file`.
- The API may be temporarily unavailable.
- If the server returns `404`, the website catalog needs the product entry and a deployment.

## No stats appear

- Statistics are rate-limited per IP/application (often one hit per hour).
- Offline/blocked networks are ignored by design.
- Check the log for `Statistics API returned ...`.

## Bug reports are not arriving

- Warnings and errors are forwarded; information/debug events are not.
- Check network access to `https://www.purelogiccode.com/bugreport/api/send-bug-report`.
- The API may rate-limit after repeated reports.
- Inspect the local log for the original event.

## Reset local state

Close the app, then delete:

- the `logs` folder
- `donation.json`

Both live under `<LocalApplicationData>/BatchConvertToCompressedFile`.
