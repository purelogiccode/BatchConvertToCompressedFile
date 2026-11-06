# CI/CD

## Workflows

| Workflow | Trigger | What it does |
| --- | --- | --- |
| `ci.yml` | push / PR / manual | Build + test on Windows, Linux, macOS; cross-publish all six RIDs |
| `release.yml` | tag `v*` / manual | Publish, package, checksum, create GitHub release |
| `codeql.yml` | push / PR / weekly | C# CodeQL analysis |
| `dependency-review.yml` | pull request | Blocks high-severity dependency changes |
| `docs.yml` | `docs/**` push / manual | Publishes the MkDocs site to GitHub Pages and syncs the GitHub Wiki |

## `ci.yml`

Matrix: `ubuntu-latest`, `windows-latest`, `macos-latest`.

Steps:

1. Checkout + .NET 10 SDK
2. `dotnet restore`
3. `dotnet build -c Release --no-restore -warnaserror`
4. `dotnet test -c Release --no-build --collect "XPlat Code Coverage"`
5. Upload coverage and test results

A second job publishes every RID with `--self-contained false` and `PublishSingleFile`, then
uploads the output as a workflow artifact. This catches cross-target regressions before a release
while matching the framework-dependent release format.

## `release.yml`

Matrix: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`, `osx-x64`, `osx-arm64`.

- Version comes from a `v*` tag or the manual `version` input.
- MSBuild overrides keep assembly/file/informational versions in sync.
- Publish uses `--self-contained false` and `PublishSingleFile`, so the .NET runtime is not
  embedded.
- `LICENSE`, `ReadMe.md`, and `WhatsNew.md` are copied into every publish folder before packaging.
- Windows payloads are zipped; Linux/macOS payloads are tarred to preserve executable bits.
- Asset names remain `BatchConvertToCompressedFile_<version>_<rid>.zip|tar.gz` for the update check.
- A `checksums.txt` file contains SHA-256 hashes for every asset.
- `softprops/action-gh-release` creates or updates the release using `WhatsNew.md` as the body.

## `codeql.yml`

Runs CodeQL's `security-and-quality` queries for C#. Permissions are limited to
`security-events: write` and `contents: read`. The configuration excludes intentional boundary
catch-all diagnostics and test-only path-combine diagnostics; production path handling is still
analyzed.

## `dependency-review.yml`

Fails a pull request when a dependency change introduces a high-severity vulnerability or a
disallowed license.

## `docs.yml`

Builds the MkDocs Material site and deploys it with the official GitHub Pages actions. The same
job copies the pages to the repository wiki, rewrites relative `.md` links to wiki links, and adds
`_Sidebar.md` and `_Footer.md` so the wiki has a persistent side menu.

Repository settings must set:

- **Pages -> Build and deployment -> Source: GitHub Actions**
- **Features -> Wikis -> enabled**

The wiki job uses `contents: write`. If the repository policy blocks `GITHUB_TOKEN` from pushing
to the wiki, add a `WIKI_TOKEN` repository secret; the workflow prefers it when present.

## Local parity

```powershell
dotnet build CSharp_BatchConvertToCompressedFile.sln -c Release
dotnet test  CSharp_BatchConvertToCompressedFile.sln -c Release
```

The release packaging can be reproduced with the publish commands in
[Building and testing](building-and-testing.md).
