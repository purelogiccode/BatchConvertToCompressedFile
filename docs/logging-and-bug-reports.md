# Logging and bug reports

## Serilog pipeline

All logging goes through Serilog. The logger is created in `LoggingSetup` and configured with:

| Sink | Minimum level | Purpose |
| --- | --- | --- |
| Debug sink | Debug | IDE / debugger output |
| Async file sink | Information | Rolling daily files, 14 retained |
| UI sink | Debug | Live log in the main window |
| Bug report sink | Warning | Forwards warnings and errors to the bug report API |

Log files are written to:

```
<LocalApplicationData>/BatchConvertToCompressedFile/logs/batch-convert-<date>.log
```

The in-app log keeps the most recent 2,000 lines.

## Automatic reporting

`BugReportApiSink` queues every event at **Warning** or higher and posts it on a background
task. The queue is bounded so a network outage cannot exhaust memory. At shutdown the sink drains
for up to 15 seconds.

Expected user conditions must be logged below Warning so they do not create bug reports:

- missing or locked files
- permission denied
- corrupt / encrypted / unsupported archives
- offline network, DNS failures, timeouts
- HTTP 429 rate limits
- user cancellation

## Report format

Every report contains the required sections:

```
=== Environment Details ===
Date: 2026-11-05 14:30:00 -03:00
Application Name: BatchConvertToCompressedFile
Application Version: 2.0.0
OS Version: Microsoft Windows NT 10.0.26100.0
Architecture: X64
Bitness: 64-bit
Windows Version: Microsoft Windows 10.0.26100
Processor Count: 16
Base Directory: C:\Apps\BatchConvertToCompressedFile\
Temp Path: C:\Users\User\AppData\Local\Temp\

=== Error Details ===
Error message: ...

=== Exception Details ===
Type: System.InvalidOperationException
Message: ...
Source: ...
StackTrace: ...
```

On Linux the platform label is `Linux Version` (with the `/etc/os-release` pretty name when
available). On macOS it is `MacOsX Version`.

Inner exceptions are appended as `--- Inner Exception ---` blocks (up to 10 levels).

## API payload

```json
{
  "message": "<full formatted report>",
  "applicationName": "BatchConvertToCompressedFile",
  "version": "2.0.0",
  "environment": "<OS description>"
}
```

The request includes the `X-API-KEY` header. The endpoint is:

```
POST https://www.purelogiccode.com/bugreport/api/send-bug-report
```

## Manual log inspection

Use **File -> Open Log Folder**. Log files are local and can be attached to a GitHub issue if a
report was not delivered automatically.

## Privacy

Reports can include file paths and stack traces. They never include archive contents. Users who
need stricter privacy can override or disable the bug report key for their environment.
