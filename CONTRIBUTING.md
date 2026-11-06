# Contributing

Thanks for improving Batch Convert to Compressed File.

## Before you start

Read [`AGENTS.md`](AGENTS.md) for the architecture and logging rules, and
[`SECURITY.md`](SECURITY.md) for the vulnerability disclosure process.

## Development setup

- .NET 10 SDK
- Windows, Linux, or macOS
- An IDE with Avalonia support (Rider, Visual Studio, or VS Code) is recommended

```powershell
git clone https://github.com/purelogiccode/BatchConvertToCompressedFile.git
cd BatchConvertToCompressedFile
dotnet build CSharp_BatchConvertToCompressedFile.sln
dotnet test CSharp_BatchConvertToCompressedFile.sln
```

## Pull requests

1. Keep changes focused and cross-platform.
2. Add or update tests for behavior changes.
3. Run `dotnet build` and `dotnet test` locally.
4. Update `docs/` and `README.md` when user-facing behavior changes.
5. Do not add native archive dependencies or live network calls in tests.

## Code style

- File-scoped namespaces, nullable enabled, implicit usings.
- Async methods end with `Async`.
- Models/enums in `Models`, interfaces in `Interfaces`.
- XML documentation on public types and members.
- Serilog is the only logging path. Expected conditions log at Information/Debug.
