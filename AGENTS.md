# Agent Instructions

Guidance for AI agents and human contributors working in this repository.

## Project

- **Application:** Batch Convert to Compressed File (`BatchConvertToCompressedFile`)
- **UI:** Avalonia 12 (Fluent dark theme with a custom black/indigo palette)
- **Target:** `net10.0` on Windows, Linux, and macOS - `x64` and `arm64`
- **Solution:** `CSharp_BatchConvertToCompressedFile.sln`
- **Tests:** `BatchConvertToCompressedFile.Tests` (xUnit)
- **Archive engine:** SharpCompress (fully managed - no native 7-Zip binaries)
- **Logging:** Serilog; warnings and errors are forwarded to the Pure Logic Code bug report API
- **Version source of truth:** `BatchConvertToCompressedFile/BatchConvertToCompressedFile.csproj`

## Build and test

```powershell
dotnet build CSharp_BatchConvertToCompressedFile.sln -c Debug
dotnet test  CSharp_BatchConvertToCompressedFile.sln -c Debug
```

Static analysis (must report zero warnings):

```powershell
jb inspectcode CSharp_BatchConvertToCompressedFile.sln --no-build --severity=WARNING --swea
roslynator analyze CSharp_BatchConvertToCompressedFile.sln --severity-level warning
```

Run the app:

```powershell
dotnet run --project BatchConvertToCompressedFile
```

Publish any supported runtime:

```powershell
dotnet publish BatchConvertToCompressedFile -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/win-x64
```

Supported RIDs: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`.

Release bundles are framework-dependent single-file builds (`--self-contained false`). They must
include `LICENSE`, `ReadMe.md`, and `WhatsNew.md`, use the
`BatchConvertToCompressedFile_<version>_<rid>.zip|tar.gz` naming pattern, and be copied to
`BatchConvertToCompressedFile\bin\Release` without deleting existing files.

## Architecture rules

- Keep **models and enums** in `BatchConvertToCompressedFile/Models`.
- Keep **interfaces** in `BatchConvertToCompressedFile/Interfaces`.
- Services live in `Services`, logging infrastructure in `Logging`, view models in `ViewModels`, and Avalonia windows in `Views`.
- All asynchronous methods must have the `Async` suffix, including `async void` event handlers.
- Default new types to `internal`. Only make a type public when it is part of the external surface or required by a framework. After changing visibility, run a full build to catch accessibility errors.
- Every public type and member must have an XML documentation comment. Internal services should be documented too.
- Remove dead code instead of leaving it commented out.
- Keep archive handling cross-platform. Do **not** reintroduce native `7z.dll` / `7z.so` dependencies.
- Never block the UI thread. File I/O and archive work run on background threads.

## Logging rules

- Route **all** logging through `Serilog` (`Log.Information`, `Log.Warning`, `Log.Error`, …). Do not add custom log/debug files or `Console.WriteLine` logging.
- `BugReportApiSink` forwards every event at **Warning or higher** to the bug report API.
- Expected user conditions (missing/locked files, permission denied, offline network, rate limits, corrupt archives, canceled operations) must be logged at **Information** or **Debug**, never Warning/Error. Otherwise they become noisy bug reports.
- The bug report body must keep the standardized sections:
  - `=== Environment Details ===` with date, application name/version, OS version, architecture, bitness, platform version, processor count, base directory, temp path.
  - `=== Error Details ===` with the error message.
  - `=== Exception Details ===` with type, message, source, and stack trace.
- Do not add PII to logs or reports beyond what the existing template requires.

## Startup integrations

- At launch the app calls `POST https://www.purelogiccode.com/ApplicationStats/stats` with the application ID and version. Do not remove this call.
- At launch the app checks the latest GitHub release and offers to open the download page when a newer version exists. It never downloads or executes an update automatically.
- The donation banner is hidden **only** after `POST https://www.purelogiccode.com/api/donations/verify` confirms a completed donation. Do not add a close/dismiss button.
- API keys have environment-variable overrides (`BUGREPORT_API_KEY`, `DONATION_API_KEY`, `APP_STATS_API_KEY`). Keep the fallback keys working for normal builds.

## Tests

- Tests must be deterministic and must never call live network endpoints. Inject `HttpMessageHandler` stubs for all HTTP services.
- Archive tests use temporary folders and real SharpCompress archives.
- Add tests for every new service, model helper, and validation rule.
- Prefer `[Theory]` for input matrices (formats, tags, invalid paths).
- CI runs the suite on Windows, Linux, and macOS.

## CI and releases

- `.github/workflows/ci.yml` - build + test on all three operating systems, then cross-publish all six RIDs.
- `.github/workflows/release.yml` - manual or tag-driven packaging of all six RIDs, SHA-256 checksums, and a GitHub release.
- `.github/workflows/codeql.yml` - CodeQL analysis for C#.
- `.github/workflows/dependency-review.yml` - blocks high-severity dependency changes in pull requests.
- `.github/workflows/docs.yml` - publishes `docs/` to GitHub Pages.
- Release asset names must keep the `_{rid}.zip` / `_{rid}.tar.gz` pattern; `UpdateService` selects assets by that suffix.

## Documentation

- User and developer documentation lives in `docs/`.
- Keep `README.md`, `AGENTS.md`, and `SECURITY.md` in sync with behavior changes.
- Do not commit machine-specific VM credentials or API keys.

## Security

Never commit private keys, credentials, database files, or production secrets. The API keys in `AppConfig` are application identification keys, not user secrets; they can be overridden at runtime. See `SECURITY.md` for the vulnerability disclosure process.
