# Architecture

## Project layout

```
BatchConvertToCompressedFile/
  App.axaml / App.axaml.cs          Composition root, global exception handling
  Program.cs                        Avalonia entry point
  AppConfig.cs                      Identity, endpoints, environment overrides
  Views/                            MainWindow, AboutWindow, MessageDialog
  ViewModels/                       MainWindowViewModel
  Services/                         Archive, batch, update, stats, donation, shell
  Logging/                          Serilog setup, bug report sink/formatter/service
  Models/                           Records and enums
  Interfaces/                       Service contracts
  Themes/DarkTheme.axaml            Black/indigo theme
BatchConvertToCompressedFile.Tests/ xUnit test suite
docs/                               Documentation
.github/workflows/                  CI, release, CodeQL, dependency review, docs
```

## Dependencies

| Package | Purpose |
| --- | --- |
| Avalonia / Avalonia.Desktop / Fluent | Cross-platform UI |
| Avalonia.Fonts.Inter | Professional typography |
| CommunityToolkit.Mvvm | Observable view model |
| SharpCompress | Managed 7z/zip creation, 7z/zip/rar verification |
| Serilog + File/Async/Debug sinks | Logging |
| xUnit + coverlet | Testing and coverage |

SharpCompress removes the need for native 7-Zip libraries on any platform.

## Startup sequence

1. `Program.Main` builds the Avalonia app.
2. `App.OnFrameworkInitializationCompleted` creates the bug report service, Serilog logger, and
   global exception handlers.
3. Services are composed and the main window is created.
4. `MainWindow.Opened` loads donation state and starts the GitHub update check.
5. In parallel, `ApplicationStatsService.RecordStartupAsync` sends the startup hit.

## Compression flow

```
MainWindowViewModel.StartCompressionAsync
  -> validates folders/options
  -> BatchArchiveProcessor.CompressBatchAsync
       -> enumerates top-level files
       -> ArchiveService.CompressFileAsync (SharpCompress writer)
       -> optional original deletion
       -> progress + statistics
  -> BatchCompleted event -> summary dialog
```

Compression runs on background tasks. Partial archives are deleted on failure or cancellation.

## Verification flow

```
MainWindowViewModel.StartVerificationAsync
  -> validates folders/options
  -> BatchArchiveProcessor.VerifyBatchAsync
       -> enumerates .7z/.zip/.rar
       -> ArchiveService.VerifyArchiveAsync
            -> reads every entry
            -> compares size and CRC-32
       -> optional move via ArchiveFileMover
  -> BatchCompleted event -> summary dialog
```

## Threading

- UI bindings update on the Avalonia dispatcher.
- `MainWindowViewModel` captures the UI `SynchronizationContext` and marshals timer updates.
- File and archive work runs via `Task.Run` or async file APIs.
- Cancellation uses `CancellationTokenSource`; a linked 10-minute timeout applies per archive.

## Visibility conventions

- Types are `internal` by default.
- Window classes are `internal` (Avalonia instantiation happens in the same assembly).
- `ArchiveFormat` is public because it is used in public test signatures.
- Public framework entry points (`App`, `Program.BuildAvaloniaApp`) are documented.

## Error handling

- Expected conditions log at Information/Debug.
- Unexpected exceptions log at Error and reach the bug report API.
- Global handlers cover AppDomain, UI dispatcher, and unobserved task exceptions.
