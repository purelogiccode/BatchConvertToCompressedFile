# FAQ

### Is the application free?

Yes. It is donation-supported. The banner disappears after a verified donation.

### Does it need 7-Zip installed?

No. Archive handling is fully managed through SharpCompress.

### Which formats can it create?

`.7z` (LZMA2) and `.zip` (Deflate).

### Which formats can it verify?

`.7z`, `.zip`, and `.rar`.

### Can it compress folders into one archive?

No. The batch operation creates one archive per file.

### Does it overwrite existing archives?

No. Existing archives are skipped.

### Can it delete the originals?

Yes, optionally, and only after successful compression.

### Does verification extract files to disk?

No. Entries are streamed and discarded after size/CRC checking.

### Why does verification take a long time?

Every byte is decompressed to validate integrity. Large or solid archives take longer. A
10-minute per-archive timeout prevents indefinite hangs.

### Does the app update itself?

No. It checks GitHub and offers to open the download page. You decide whether to download.

### What data is sent to the internet?

- Startup: application ID and version (statistics)
- Update check: GitHub release request
- Warning+ events: formatted bug report
- Donation verification: the email you enter

### Where are logs stored?

Under `<LocalApplicationData>/BatchConvertToCompressedFile/logs`.

### Can I run it on a server without a desktop?

The UI requires a graphical session. The test suite builds on headless CI but does not launch the
window.

### Is there a CLI?

Not currently. The application is GUI-only.

### How do I report a bug?

Warnings and errors are reported automatically. You can also open an issue at
<https://github.com/purelogiccode/BatchConvertToCompressedFile/issues>. Report security issues
privately via [SECURITY.md](https://github.com/purelogiccode/BatchConvertToCompressedFile/blob/master/SECURITY.md).
