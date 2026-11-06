using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Models;

namespace BatchConvertToCompressedFile.Services;

/// <summary>Checks GitHub releases for a newer application version.</summary>
internal sealed class UpdateService : IUpdateService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _updateUrl;
    private readonly Version _currentVersion;
    private readonly string _runtimeIdentifier;
    private readonly bool _ownsClient;

    /// <summary>Initializes a new update service.</summary>
    /// <param name="updateUrl">GitHub latest-release API URL.</param>
    /// <param name="httpClient">Optional HTTP client for tests.</param>
    /// <param name="currentVersion">Optional current version override for tests.</param>
    /// <param name="runtimeIdentifier">Optional RID override for tests.</param>
    public UpdateService(
        string updateUrl,
        HttpClient? httpClient = null,
        Version? currentVersion = null,
        string? runtimeIdentifier = null)
    {
        _updateUrl = updateUrl;
        _currentVersion = currentVersion ??
                          Assembly.GetExecutingAssembly().GetName().Version ??
                          new Version(0, 0, 0, 0);
        _runtimeIdentifier = runtimeIdentifier ?? AppConfig.RuntimeIdentifier;

        _httpClient = httpClient ?? new HttpClient();
        _ownsClient = httpClient is null;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            $"{AppConfig.ApplicationName}/{AppConfig.ApplicationVersionString}");
        _httpClient.DefaultRequestHeaders.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    /// <inheritdoc />
    public async Task<UpdateCheckResult> CheckForUpdateAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));

            using var response = await _httpClient.GetAsync(_updateUrl, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                Log.Information(
                    "Update check returned {StatusCode}; treating the application as up to date",
                    (int)response.StatusCode);
                return UpdateCheckResult.UpToDate;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(timeoutCts.Token);
            var release = await JsonSerializer.DeserializeAsync<GitHubRelease>(
                stream, cancellationToken: timeoutCts.Token);

            if (release?.TagName is null)
            {
                Log.Information("Update check returned no release tag");
                return UpdateCheckResult.UpToDate;
            }

            var latestVersion = ParseVersion(release.TagName);
            if (latestVersion is null)
            {
                Log.Information("Could not parse release tag {TagName}", release.TagName);
                return UpdateCheckResult.UpToDate;
            }

            if (Normalize(latestVersion) <= Normalize(_currentVersion))
            {
                Log.Information(
                    "Application is up to date (running {CurrentVersion}, latest {LatestVersion})",
                    _currentVersion, latestVersion);
                return UpdateCheckResult.UpToDate;
            }

            var asset = FindAssetForCurrentPlatform(release.Assets);
            Log.Information(
                "Update available: {LatestVersion} (running {CurrentVersion}), asset {AssetName}",
                latestVersion, _currentVersion, asset?.Name ?? "none");

            return new UpdateCheckResult(
                true,
                latestVersion.ToString(3),
                release.HtmlUrl ?? AppConfig.ReleasesUrl,
                asset?.BrowserDownloadUrl,
                asset?.Name);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            // Offline machines, DNS failures, and proxy/WAF responses are expected conditions.
            Log.Information("Update check failed: {Message}", ex.Message);
            return UpdateCheckResult.UpToDate;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during update check");
            return UpdateCheckResult.UpToDate;
        }
    }

    /// <summary>Extracts a semantic version from tags such as <c>v2.0.0</c> or <c>release_2.0.0</c>.</summary>
    /// <param name="tagName">Release tag.</param>
    /// <returns>The parsed version, or <see langword="null"/>.</returns>
    internal static Version? ParseVersion(string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName)) return null;

        var candidate = tagName.Trim();
        var underscore = candidate.LastIndexOf('_');
        if (underscore >= 0) candidate = candidate[(underscore + 1)..];
        var slash = candidate.LastIndexOf('/');
        if (slash >= 0) candidate = candidate[(slash + 1)..];
        if (candidate.StartsWith('v') || candidate.StartsWith('V')) candidate = candidate[1..];

        return Version.TryParse(candidate, out var version) ? version : null;
    }

    /// <summary>Gets the release asset suffixes accepted for a runtime identifier.</summary>
    /// <param name="runtimeIdentifier">Runtime identifier such as <c>linux-arm64</c>.</param>
    /// <returns>Suffixes ordered by preference.</returns>
    internal static IReadOnlyList<string> GetAssetSuffixes(string runtimeIdentifier)
    {
        var suffixes = new List<string>
        {
            $"_{runtimeIdentifier}.zip",
            $"_{runtimeIdentifier}.tar.gz"
        };

        if (runtimeIdentifier.StartsWith("linux-", StringComparison.OrdinalIgnoreCase) ||
            runtimeIdentifier.StartsWith("osx-", StringComparison.OrdinalIgnoreCase))
        {
            suffixes.Reverse();
        }

        return suffixes;
    }

    private GitHubAsset? FindAssetForCurrentPlatform(List<GitHubAsset>? assets)
    {
        if (assets is null || assets.Count == 0) return null;

        foreach (var suffix in GetAssetSuffixes(_runtimeIdentifier))
        {
            var match = assets.FirstOrDefault(asset =>
                !string.IsNullOrWhiteSpace(asset.Name) &&
                asset.Name.EndsWith(suffix, StringComparison.OrdinalIgnoreCase));
            if (match is not null) return match;
        }

        return null;
    }

    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, 0), Math.Max(version.Revision, 0));

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsClient) _httpClient.Dispose();
    }
}
