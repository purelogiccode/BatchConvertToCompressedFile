using System.Text.Json.Serialization;

namespace BatchConvertToCompressedFile.Models;

/// <summary>Represents a GitHub release API response.</summary>
/// <param name="TagName">Release tag, for example <c>v2.0.0</c> or <c>release_2.0.0</c>.</param>
/// <param name="HtmlUrl">Browser URL of the release.</param>
/// <param name="Assets">Assets attached to the release.</param>
internal sealed record GitHubRelease(
    [property: JsonPropertyName("tag_name")] string? TagName,
    [property: JsonPropertyName("html_url")] string? HtmlUrl,
    [property: JsonPropertyName("assets")] List<GitHubAsset>? Assets);

/// <summary>Represents a single asset within a GitHub release.</summary>
/// <param name="Name">Asset file name.</param>
/// <param name="BrowserDownloadUrl">Direct browser download URL.</param>
internal sealed record GitHubAsset(
    [property: JsonPropertyName("name")] string? Name,
    [property: JsonPropertyName("browser_download_url")] string? BrowserDownloadUrl);
