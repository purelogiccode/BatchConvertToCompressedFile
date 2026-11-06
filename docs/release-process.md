# Release process

## Versioning

Update the canonical version in `BatchConvertToCompressedFile/BatchConvertToCompressedFile.csproj`:

```xml
<Version>2.0.0</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<FileVersion>2.0.0.0</FileVersion>
<InformationalVersion>2.0.0</InformationalVersion>
```

Tags use the `v<version>` form, for example `v2.0.0`. The in-app update check also accepts legacy
`release_<version>` tags.

## Standard release

1. Merge all changes to the default branch.
2. Run the full suite locally:
   ```powershell
   dotnet test CSharp_BatchConvertToCompressedFile.sln -c Release
   jb inspectcode CSharp_BatchConvertToCompressedFile.sln --no-build --severity=WARNING --swea
   ```
3. Create and push a tag:
   ```powershell
   git tag v2.0.0
   git push origin v2.0.0
   ```
4. `release.yml` publishes all six RIDs, packages them, writes `checksums.txt`, and creates the
   GitHub release.
5. The Documentation workflow deploys the MkDocs site to GitHub Pages and syncs the same pages to
   the GitHub Wiki, including `_Sidebar.md`.
6. Verify each asset exists and the checksums match.
7. Confirm `UpdateService` can find the new release:
   ```powershell
   curl.exe -s https://api.github.com/repos/purelogiccode/BatchConvertToCompressedFile/releases/latest
   ```

## Manual release

Use **Actions -> Release -> Run workflow**, enter `2.0.0`, and run it from the default branch.

## Bundle contents

Every release bundle contains:

```
BatchConvertToCompressedFile(.exe)
LICENSE
ReadMe.md
WhatsNew.md
```

The executable is a **framework-dependent single-file** build:

```powershell
dotnet publish BatchConvertToCompressedFile -c Release -r "$RID" --self-contained false `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true `
  -p:DebugType=embedded -p:Version="$VERSION" -o "artifacts/$RID"
```

The .NET runtime is **not** embedded. Users install the .NET 10 runtime separately.

## Asset naming

```
BatchConvertToCompressedFile_2.0.0_win-x64.zip
BatchConvertToCompressedFile_2.0.0_win-arm64.zip
BatchConvertToCompressedFile_2.0.0_linux-x64.tar.gz
BatchConvertToCompressedFile_2.0.0_linux-arm64.tar.gz
BatchConvertToCompressedFile_2.0.0_osx-x64.tar.gz
BatchConvertToCompressedFile_2.0.0_osx-arm64.tar.gz
checksums.txt
```

The in-app update check matches `_<rid>.zip` or `_<rid>.tar.gz`, so do not rename these assets.

## Local release copies

The release script also writes a copy of every bundle to:

```
BatchConvertToCompressedFile\bin\Release
```

Existing files in that folder are never deleted. The script writes
`BatchConvertToCompressedFile_2.0.0_<rid>.zip|tar.gz` and
`checksums_2.0.0.txt`.

## Post-release checks

- Download each asset and extract it.
- Confirm each bundle contains `LICENSE`, `ReadMe.md`, and `WhatsNew.md`.
- Launch at least the Windows and Linux x64 builds with the .NET 10 runtime installed.
- Confirm the About window shows the new version.
- Confirm a deliberately older build offers the update.
- Confirm the donation banner is hidden after a verified donation.
- Confirm a startup stats hit appears in the dashboard.
- Confirm a Warning+ log event produces a bug report.
- Confirm the GitHub Pages site and Wiki both show the current documentation and sidebar.

## Rollback

If a release is broken:

1. Delete or mark the GitHub release as a draft.
2. Publish a fixed patch release with a higher version.
3. Never reuse a version number; clients only compare version numbers.
