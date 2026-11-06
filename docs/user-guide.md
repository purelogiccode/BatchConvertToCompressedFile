# User guide

## Main window

The window has three areas:

1. **Menu** - log folder, exit, update check, GitHub, donation, and About.
2. **Left side** - compression and verification tabs.
3. **Right side** - a resizable live log (drag the splitter).
4. **Bottom** - statistics cards, progress, and the status bar.

Every interactive control has a tooltip explaining what it does.

## Compression

| Option | Behavior |
| --- | --- |
| Source folder | Files directly inside this folder are processed (top level only) |
| Output folder | Created automatically if it does not exist |
| `.7z` | 7-Zip container with LZMA2 compression |
| `.zip` | Standard ZIP container with Deflate compression |
| Delete originals | Deletes a source file only after its archive was created successfully |

Rules:

- Input and output folders must be different.
- A file whose output archive already exists is **skipped**, not overwritten.
- Archive names use the source file name with invalid characters replaced by `_`.
- Files locked by another process are logged and counted as failed; the batch continues.

## Verification

Supported formats: `.7z`, `.zip`, `.rar`.

| Option | Behavior |
| --- | --- |
| Include subfolders | Recursively scans the archives folder |
| Move successful archives | Moves verified archives to the success folder |
| Move failed archives | Moves failed archives to the failed folder |

Rules:

- Success and failed folders must be different from each other and from the input folder.
- Subfolder structure is preserved when **Include subfolders** is enabled.
- Existing destination files are never overwritten; the move is skipped.
- Move retries handle transient file locks.
- Encrypted archives without a password are treated as failed.
- A per-archive timeout of 10 minutes prevents an indefinitely hanging archive.

## Progress and statistics

| Card | Meaning |
| --- | --- |
| TOTAL FILES | Files included in the current batch |
| SUCCESS | Files processed successfully |
| FAILED | Files that failed |
| ELAPSED | Wall-clock time since the batch started |
| WRITE SPEED | Approximate throughput based on completed output/moved bytes |

The **Cancel** button stops the batch after the active file finishes. No partially created
archive is left behind: partial output is deleted.

## Live log

The in-app log shows application and operation events. The same events are written by Serilog to
the rolling log file. Use **Open log folder** to inspect older sessions.

## Updates

At launch the app asks GitHub for the latest release:

- If a newer version exists, a dialog shows the version and platform and offers to open the
  download page.
- If you decline, nothing is downloaded or executed.
- **Help -> Check for Updates** performs the same check manually and always reports the result.

## Donation banner

The application is donation-supported. A banner at the top of the window contains:

- **Donate** - opens the Pure Logic Code donation page for this application.
- **Email used for donation** - the address used on the donation page.
- **Verify donation** - calls the donation API and hides the banner permanently after a
  completed donation is confirmed.

The banner has **no close button** and disappears only after successful verification. See
[Donation](donation.md) for details.

## Keyboard and accessibility

- Tab navigation follows the visual order.
- Buttons, inputs, checkboxes, radio buttons, tabs, menus, progress, and status elements expose
  descriptive tooltips.
- The dark theme keeps high contrast for text and status colors.
