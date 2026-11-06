# Security Policy

## Supported versions

| Version | Supported |
| ------- | --------- |
| 2.x     | Yes       |
| 1.x     | No        |

## Reporting a vulnerability

Please do **not** open a public issue for a security vulnerability.

Use GitHub's private vulnerability reporting:

1. Open <https://github.com/purelogiccode/BatchConvertToCompressedFile/security/advisories/new>.
2. Describe the issue, affected version/platform, reproduction steps, and impact.
3. Include a proof of concept when possible.

If private reporting is unavailable, contact the maintainer through
<https://www.purelogiccode.com> and ask for a private security channel.

You can expect an initial acknowledgement within **5 business days** and a status update
within **10 business days**. Please allow time for a fix before public disclosure.

## Scope

In scope:

- The desktop application and its source code in this repository.
- Archive parsing/creation paths (SharpCompress), file movement, and path handling.
- Update checks, statistics, bug report, and donation verification clients.
- Build, release, and GitHub Actions workflows in this repository.

Out of scope:

- Vulnerabilities in third-party services (GitHub, Pure Logic Code hosting) that are not
  caused by this project's integration code.
- Social engineering, physical attacks, or denial-of-service against public APIs.
- Issues that require an attacker to already control the user's machine or account.

## Security design

- **Managed archive engine:** SharpCompress is fully managed. The app no longer ships or loads
  native `7z.dll` / `7z.so` binaries, which removes a native-code attack surface.
- **No silent updates:** the app checks the latest GitHub release and only opens a browser page
  after user confirmation. It never downloads or executes an update automatically.
- **TLS:** all remote calls use HTTPS with the platform certificate store.
- **Anonymous statistics:** only the application ID and version are sent to the statistics API.
- **Bug reports:** warnings and errors are forwarded with environment details and stack traces.
  Reports can contain file paths; users who require stricter privacy should run the app with
  `BUGREPORT_API_KEY` set to an empty/disabled value in a controlled environment.
- **Donation state:** verification is server-side. A local `donation.json` only caches a
  successful server response; deleting it re-shows the banner.
- **API keys:** the keys compiled into the app identify the application to Pure Logic Code
  services. They are not user credentials. Override them with `BUGREPORT_API_KEY`,
  `DONATION_API_KEY`, and `APP_STATS_API_KEY` when needed.

## Supply chain and repository hardening

- Dependabot monitors NuGet and GitHub Actions dependencies weekly.
- CodeQL analyzes C# on every push, pull request, and weekly schedule.
- Dependency Review blocks pull requests that introduce high-severity vulnerabilities.
- NuGet audit runs during restore with `NuGetAuditMode=all`.
- GitHub Actions use least-privilege `permissions` blocks.
- Release artifacts include SHA-256 checksums.

## Safe usage recommendations

- Download releases only from the official GitHub repository.
- Verify the SHA-256 checksums published with each release.
- Keep your operating system and .NET runtime up to date.
- Do not run the application with elevated privileges unless required by the folders you process.
