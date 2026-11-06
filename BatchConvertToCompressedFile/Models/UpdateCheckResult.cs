namespace BatchConvertToCompressedFile.Models;

/// <summary>Represents the result of an update check.</summary>
/// <param name="IsNewVersionAvailable">Indicates whether a newer version exists.</param>
/// <param name="LatestVersion">Version number of the latest release.</param>
/// <param name="ReleaseUrl">URL of the GitHub release page.</param>
/// <param name="DownloadUrl">Direct download URL for the current platform, when available.</param>
/// <param name="AssetName">Name of the matching release asset.</param>
internal sealed record UpdateCheckResult(
    bool IsNewVersionAvailable,
    string LatestVersion,
    string? ReleaseUrl,
    string? DownloadUrl,
    string? AssetName)
{
    /// <summary>Gets a result indicating that the application is up to date.</summary>
    public static UpdateCheckResult UpToDate { get; } = new(false, AppConfig.ApplicationVersionString, null, null, null);
}
