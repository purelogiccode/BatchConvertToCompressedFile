# CI/CD

## Workflows

| Workflow | Trigger | What it does |
| --- | --- | --- |
| `ci.yml` | push / PR / manual | Build + test on Windows, Linux, macOS; cross-publish all six RIDs |
| `release.yml` | tag `v*` / manual | Publish, package, checksum, create GitHub release |
| `codeql.yml` | push / PR / weekly | C# CodeQL analysis |
| `dependency-review.yml` | pull request | Blocks high-severity dependency changes |
| `docs.yml` | `docs/**` push / manual | Publishes `docs/` to GitHub Pages |

## `ci.yml`

Matrix: `ubuntu-latest`, `windows-latest`, `macos-latest`.

Steps:

1. Checkout + .NET 10 SDK
2. `dotnet restore`
3. `dotnet build -c Release --no-restore`
4. `dotnet test -c Release --no-build --collect "XPlat Code Coverage"`
5. Upload coverage and test results

A second job publishes every RID with `--self-contained` and `PublishSingleFile`, then uploads
the output as a workflow artifact. This catches cross-target regressions before a release.

## `release.yml`

Matrix: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`.

- Version comes from a `v*` tag or the manual `version` input.
- MSBuild overrides keep assembly/file/informational versions in sync.
- Windows payloads are zipped; Linux/macOS payloads are tarred to preserve executable bits.
- A `checksums.txt` file contains SHA-256 hashes for every asset.
- `softprops/action-gh-release` creates or updates the release with generated notes.

## `codeql.yml`

Runs CodeQL's `security-and-quality` queries for C#. Permissions are limited to
`security-events: write` and `contents: read`.

## `dependency-review.yml`

Fails a pull request when a dependency change introduces a high-severity vulnerability or a
disallowed license.

## `docs.yml`

Uploads `docs/` as a Pages artifact and deploys it with the official GitHub Pages actions.
Repository settings must set **Pages -> Build and deployment -> Source: GitHub Actions**.

## Local parity

```powershell
dotnet build CSharp_BatchConvertToCompressedFile.sln -c Release
dotnet test  CSharp_BatchConvertToCompressedFile.sln -c Release
```

The release packaging can be reproduced with the publish commands in
[Building and testing](building-and-testing.md).
