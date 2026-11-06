# Manual test checklist

Automated tests cover archive logic, API clients, logging, and view-model rules. This checklist
covers UI and platform smoke tests that need a real desktop session.

## Test matrix

| Platform | Architecture | Automated tests | UI smoke | Release archive |
| --- | --- | --- | --- | --- |
| Windows | x64 | CI | Required | ZIP |
| Windows | arm64 | CI build | When hardware available | ZIP |
| Linux | x64 | CI + WSL | Required | TAR.GZ |
| Linux | arm64 | CI build | When hardware available | TAR.GZ |
| macOS | x64 | CI | When hardware available | TAR.GZ |
| macOS | arm64 | CI | When hardware available | TAR.GZ |

## Automated suite

```powershell
dotnet test CSharp_BatchConvertToCompressedFile.sln -c Release
```

Expected: **91 tests passed, 0 failed** (count grows as tests are added).

## Windows UI

1. Launch `BatchConvertToCompressedFile.exe`.
2. Confirm the black/indigo theme, donation banner, left tabs, right live log, statistics row.
3. Hover a sample of controls and confirm every element shows a tooltip.
4. Compress two small files to `.zip`; confirm stats, progress, and log entries.
5. Compress one file to `.7z`; verify the archive.
6. Verify a corrupt archive; confirm it is counted as failed.
7. Open **Help -> About**; confirm version, platform, runtime, and links.
8. Open **Help -> Check for Updates**; confirm the result dialog.
9. Resize the window and drag the log splitter; confirm layout remains usable.
10. Close the app; confirm the log file is flushed.

## Linux VM

The Hyper-V VM used for validation is `D:\Hyper-V\VMs\LinuxMint`. Machine-specific SSH details are
kept outside this repository (see the local agent notes for the launcher project).

1. Start the VM from an elevated PowerShell session:
   ```powershell
   Start-VM -Name LinuxMint
   ```
2. Find the current address:
   ```powershell
   Get-NetNeighbor -InterfaceAlias 'vEthernet (Default Switch)'
   ```
3. Copy the published Linux build to the guest:
   ```bash
   scp BatchConvertToCompressedFile_*_linux-x64.tar.gz vm@<vm-ip>:~/
   ssh vm@<vm-ip>
   tar -xzf BatchConvertToCompressedFile_*_linux-x64.tar.gz -C ~/bcctf
   chmod +x ~/bcctf/BatchConvertToCompressedFile
   ```
4. Launch it inside the guest desktop session. If testing over SSH, use the X server or the
   repository smoke scripts:
   ```bash
   ./scripts/test-linux-ui.sh ~/bcctf/BatchConvertToCompressedFile linux-ui.png
   ./scripts/test-linux-about.sh ~/bcctf/BatchConvertToCompressedFile linux-about.png
   ```
5. Verify the window appears at 1080x780, with the log on the right and statistics at the bottom.
6. Repeat the compression/verification checks from the Windows list.
7. Confirm the Open Log Folder action opens the file manager.
8. Confirm the About window opens and displays the Linux distribution and architecture.

## macOS

1. Extract the `.tar.gz` and run `./BatchConvertToCompressedFile`.
2. If Gatekeeper blocks the unsigned development build, approve it in
   **System Settings -> Privacy & Security**.
3. Repeat the compression, verification, About, and update checks.

## Donation verification

Requires a deployed website catalog entry with slug `batch-convert-to-compressed-file`.

1. Click **Donate** and confirm the donation page opens with the application preselected.
2. Enter the donation email in the banner and click **Verify donation**.
3. A completed donation hides the banner and writes `donation.json`.
4. A non-donated email keeps the banner visible and shows a friendly message.
5. Restart the app; the banner stays hidden after a verified donation.

## Statistics and bug reports

- Launch the app and check the log for `Startup statistics recorded` (or an expected rate-limit
  message).
- Trigger a real Warning/Error condition (for example, temporarily point the output folder at an
  unwritable location and cause an unexpected failure) and confirm a report is submitted.
- Expected user errors (locked files, offline network, corrupt archives) must **not** generate
  reports.
