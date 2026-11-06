## Summary

Describe the change and why it is needed.

## Checklist

- [ ] `dotnet build CSharp_BatchConvertToCompressedFile.sln -c Release` passes
- [ ] `dotnet test CSharp_BatchConvertToCompressedFile.sln -c Release` passes
- [ ] New behavior has tests
- [ ] Async methods end with `Async`
- [ ] Models/enums are in `Models`, interfaces in `Interfaces`
- [ ] Expected user errors log at Information/Debug (not Warning/Error)
- [ ] Documentation updated when user-facing behavior changed
- [ ] No new native archive dependency or live network call in tests

## Platform testing

- [ ] Windows x64
- [ ] Windows arm64 (if available)
- [ ] Linux x64
- [ ] Linux arm64 (if available)
- [ ] macOS x64
- [ ] macOS arm64 (if available)
