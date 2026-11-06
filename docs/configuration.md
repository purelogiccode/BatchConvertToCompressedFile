# Configuration

The application has no user-editable settings file. Behavior is controlled by constants in
`BatchConvertToCompressedFile/AppConfig.cs` and optional environment variables.

## Environment variables

| Variable | Purpose | Default |
| --- | --- | --- |
| `BUGREPORT_API_KEY` | API key for the bug report endpoint | Application key |
| `DONATION_API_KEY` | API key for donation verification | Application key |
| `APP_STATS_API_KEY` | Bearer token for application statistics | Application key |

Set a variable in the environment that launches the application:

```powershell
$env:BUGREPORT_API_KEY = "my-key"
dotnet run --project BatchConvertToCompressedFile
```

```bash
export APP_STATS_API_KEY="my-key"
./BatchConvertToCompressedFile
```

## Endpoints

| Feature | Endpoint |
| --- | --- |
| Update check | `https://api.github.com/repos/purelogiccode/BatchConvertToCompressedFile/releases/latest` |
| Bug reports | `https://www.purelogiccode.com/bugreport/api/send-bug-report` |
| Statistics | `https://www.purelogiccode.com/ApplicationStats/stats` |
| Donation verification | `https://www.purelogiccode.com/api/donations/verify` |
| Donation page | `https://www.purelogiccode.com/Donate?app=batch-convert-to-compressed-file` |

## Identifiers

| Identifier | Value |
| --- | --- |
| Application name | `BatchConvertToCompressedFile` |
| Statistics application ID | `batchconverttocompressedfile` |
| Donation application slug | `batch-convert-to-compressed-file` |
| GitHub repository | `purelogiccode/BatchConvertToCompressedFile` |

## Local data

- Logs: `<LocalApplicationData>/BatchConvertToCompressedFile/logs`
- Donation cache: `<LocalApplicationData>/BatchConvertToCompressedFile/donation.json`

`LocalApplicationData` resolves to `%LOCALAPPDATA%` on Windows and `~/.local/share` on Linux.
On macOS the .NET runtime maps it to the user's application-support area.

## Proxy and TLS

HTTP requests use the platform defaults, including system proxy settings and the OS certificate
store. No certificate validation is bypassed.

## Version

The canonical version is in `BatchConvertToCompressedFile/BatchConvertToCompressedFile.csproj`:

```xml
<Version>2.0.0</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<FileVersion>2.0.0.0</FileVersion>
```

Release workflows can override it with MSBuild properties.
