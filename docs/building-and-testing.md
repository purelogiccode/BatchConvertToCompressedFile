# Building and testing

## Prerequisites

- .NET 10 SDK (`dotnet --version` should report 10.x)
- Windows, Linux, or macOS
- No native archive tools are required

## Restore, build, test

```powershell
dotnet restore CSharp_BatchConvertToCompressedFile.sln
dotnet build   CSharp_BatchConvertToCompressedFile.sln -c Release --no-restore
dotnet test    CSharp_BatchConvertToCompressedFile.sln -c Release --no-build
```

Run a single test:

```powershell
dotnet test BatchConvertToCompressedFile.Tests --filter "FullyQualifiedName~ArchiveServiceTests"
```

Collect coverage:

```powershell
dotnet test CSharp_BatchConvertToCompressedFile.sln -c Release `
  --collect:"XPlat Code Coverage" --results-directory TestResults
```

## Run locally

```powershell
dotnet run --project BatchConvertToCompressedFile
```

## Publish

All six supported RIDs:

```powershell
$rids = 'win-x64','win-arm64','linux-x64','linux-arm64','osx-x64','osx-arm64'
foreach ($rid in $rids) {
  dotnet publish BatchConvertToCompressedFile -c Release -r $rid `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=embedded `
    -o "artifacts/$rid"
}
```

The .NET runtime is not embedded; install the .NET 10 runtime on the target machine.

Test framework-dependent publish:

```powershell
dotnet publish BatchConvertToCompressedFile -c Release -r linux-x64 --self-contained false
```

## Package

| Platform | Format | Command |
| --- | --- | --- |
| Windows | `.zip` | `Compress-Archive -Path artifacts/win-x64/* -DestinationPath BatchConvertToCompressedFile_2.0.0_win-x64.zip` |
| Linux/macOS | `.tar.gz` | `tar -czf BatchConvertToCompressedFile_2.0.0_linux-x64.tar.gz -C artifacts/linux-x64 .` |

Before packaging, copy `LICENSE`, `ReadMe.md`, and `WhatsNew.md` into each publish folder. Every
bundle should contain the single application executable plus those three documents.

Release workflow names:

```
BatchConvertToCompressedFile_<version>_<rid>.zip
BatchConvertToCompressedFile_<version>_<rid>.tar.gz
```

`UpdateService` selects assets by the `_<rid>.zip` / `_<rid>.tar.gz` suffix.

## Test suite

The suite covers:

- zip and 7z creation/readback
- corrupt archive detection with CRC-32
- batch compression, skip, delete-original, empty folder, missing folder
- duplicate archive names, dotfiles, and portable file-name sanitization
- cancellation cleanup with no partial archives
- verification success/failure, supported extensions, subfolder moves
- path sanitization, containment, relative paths, and platform case sensitivity
- update tag parsing, asset selection, HTTP failures, timeout, and caller cancellation
- donation success/failure/404/401/network/empty email
- donation state persistence and corrupt state
- statistics payload and error handling
- bug report format and sink level filtering
- view-model validation, donation banner, progress counts, and operation completion

Tests never call live APIs; HTTP is stubbed with a custom `HttpMessageHandler`.

## Static analysis

The repository is kept clean under JetBrains InspectCode and Roslynator:

```powershell
jb inspectcode CSharp_BatchConvertToCompressedFile.sln `
  --no-build --severity=WARNING --swea
roslynator analyze CSharp_BatchConvertToCompressedFile.sln --severity-level warning
```

Both commands must report zero warnings before a change is merged. Every `async void` event handler
must wrap its entire body in `try/catch` so a UI exception can never terminate the process.

## Linux UI smoke tests

The repository includes two scripts that launch the published Linux build inside a virtual X
server and capture screenshots. They require `xvfb`, `xdotool`, and ImageMagick:

```bash
./scripts/test-linux-ui.sh artifacts/linux-x64/BatchConvertToCompressedFile artifacts/linux-ui.png
./scripts/test-linux-about.sh artifacts/linux-x64/BatchConvertToCompressedFile artifacts/linux-about.png
```

They are useful for a Linux VM or WSL validation and are not part of CI.

## Platform notes

- Windows: the app uses `WinExe` but still opens a console-free UI.
- Linux: run inside a graphical session. Headless CI only builds and tests; it does not launch the
  UI.
- macOS: unsigned development builds may require Gatekeeper approval. Official releases are
  packaged in `tar.gz` to preserve executable permissions.
