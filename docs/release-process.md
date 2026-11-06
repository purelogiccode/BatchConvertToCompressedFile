# Release process

## Versioning

Update the canonical version in `BatchConvertToCompressedFile/BatchConvertToCompressedFile.csproj`:

```xml
<Version>2.0.0</Version>
<AssemblyVersion>2.0.0.0</AssemblyVersion>
<FileVersion>2.0.0.0</FileVersion>
<InformationalVersion>2.0.0</InformationalVersion>
```

Tags use the `v<version>` form, for example `v2.0.0`.

## Standard release

1. Merge all changes to the default branch.
2. Run the full suite locally:
   ```powershell
   dotnet test CSharp_BatchConvertToCompressedFile.sln -c Release
   ```
3. Create and push a tag:
   ```powershell
   git tag v2.0.0
   git push origin v2.0.0
   ```
4. `release.yml` publishes all six RIDs, packages them, writes `checksums.txt`, and creates the
   GitHub release.
5. Verify each asset exists and the checksums match.
6. Confirm `UpdateService` can find the new release:
   ```powershell
   curl.exe -s https://api.github.com/repos/purelogiccode/BatchConvertToCompressedFile/releases/latest
   ```

## Manual release

Use **Actions -> Release -> Run workflow**, enter `2.0.0`, and run it from the default branch.

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

The in-app update check matches `_<rid>.zip` or `_<rid>.tar.gz`.

## Post-release checks

- Download each asset and extract it.
- Launch at least the Windows and Linux x64 builds.
- Confirm the About window shows the new version.
- Confirm a deliberately older build offers the update.
- Confirm the donation banner is hidden after a verified donation.
- Confirm a startup stats hit appears in the dashboard.
- Confirm a Warning+ log event produces a bug report.

## Rollback

If a release is broken:

1. Delete or mark the GitHub release as a draft.
2. Publish a fixed patch release with a higher version.
3. Never reuse a version number; clients only compare version numbers.
