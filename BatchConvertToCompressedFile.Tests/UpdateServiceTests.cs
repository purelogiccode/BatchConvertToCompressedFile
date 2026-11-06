using System.Net;
using System.Text.Json;
using BatchConvertToCompressedFile.Models;
using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class UpdateServiceTests
{
    private const string NewerReleaseJson = """
        {
          "tag_name": "release_99.1.2",
          "html_url": "https://github.com/purelogiccode/BatchConvertToCompressedFile/releases/tag/99.1.2",
          "assets": [
            {
              "name": "BatchConvertToCompressedFile_99.1.2_win-x64.zip",
              "browser_download_url": "https://example.test/win-x64.zip"
            },
            {
              "name": "BatchConvertToCompressedFile_99.1.2_linux-arm64.tar.gz",
              "browser_download_url": "https://example.test/linux-arm64.tar.gz"
            }
          ]
        }
        """;

    [Fact]
    public async Task CheckForUpdateAsync_NewerVersion_ReturnsUpdateWithMatchingAsset()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Json(HttpStatusCode.OK, NewerReleaseJson)));
        using var client = new HttpClient(handler);
        using var service = new UpdateService(
            "https://api.github.test/latest", client, new Version(1, 0, 0, 0), "win-x64");

        var result = await service.CheckForUpdateAsync();

        Assert.True(result.IsNewVersionAvailable);
        Assert.Equal("99.1.2", result.LatestVersion);
        Assert.Equal("https://example.test/win-x64.zip", result.DownloadUrl);
        Assert.Equal("BatchConvertToCompressedFile_99.1.2_win-x64.zip", result.AssetName);
        Assert.Contains("releases/tag", result.ReleaseUrl);
    }

    [Fact]
    public async Task CheckForUpdateAsync_LinuxArm64_SelectsTarGzAsset()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Json(HttpStatusCode.OK, NewerReleaseJson)));
        using var client = new HttpClient(handler);
        using var service = new UpdateService(
            "https://api.github.test/latest", client, new Version(1, 0, 0, 0), "linux-arm64");

        var result = await service.CheckForUpdateAsync();

        Assert.True(result.IsNewVersionAvailable);
        Assert.Equal("https://example.test/linux-arm64.tar.gz", result.DownloadUrl);
    }

    [Fact]
    public async Task CheckForUpdateAsync_SameVersion_ReturnsUpToDate()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Json(HttpStatusCode.OK, NewerReleaseJson)));
        using var client = new HttpClient(handler);
        using var service = new UpdateService(
            "https://api.github.test/latest", client, new Version(99, 1, 2, 0), "win-x64");

        var result = await service.CheckForUpdateAsync();

        Assert.False(result.IsNewVersionAvailable);
    }

    [Fact]
    public async Task CheckForUpdateAsync_HttpFailure_ReturnsUpToDate()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.Forbidden)));
        using var client = new HttpClient(handler);
        using var service = new UpdateService(
            "https://api.github.test/latest", client, new Version(1, 0, 0, 0), "win-x64");

        var result = await service.CheckForUpdateAsync();

        Assert.False(result.IsNewVersionAvailable);
    }

    [Fact]
    public async Task CheckForUpdateAsync_NetworkFailure_ReturnsUpToDate()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new HttpRequestException("offline"));
        using var client = new HttpClient(handler);
        using var service = new UpdateService(
            "https://api.github.test/latest", client, new Version(1, 0, 0, 0), "win-x64");

        var result = await service.CheckForUpdateAsync();

        Assert.False(result.IsNewVersionAvailable);
    }

    [Fact]
    public async Task CheckForUpdateAsync_InvalidJson_ReturnsUpToDate()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Json(HttpStatusCode.OK, "{not-json")));
        using var client = new HttpClient(handler);
        using var service = new UpdateService(
            "https://api.github.test/latest", client, new Version(1, 0, 0, 0), "win-x64");

        var result = await service.CheckForUpdateAsync();

        Assert.False(result.IsNewVersionAvailable);
    }

    [Theory]
    [InlineData("v1.2.3", "1.2.3")]
    [InlineData("V2.0.0", "2.0.0")]
    [InlineData("release_3.4.5", "3.4.5")]
    [InlineData("refs/tags/v4.5.6", "4.5.6")]
    [InlineData("5.0", "5.0")]
    public void ParseVersion_HandlesCommonTagFormats(string tag, string expected)
    {
        Assert.Equal(Version.Parse(expected), UpdateService.ParseVersion(tag));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-a-version")]
    public void ParseVersion_InvalidTag_ReturnsNull(string tag)
    {
        Assert.Null(UpdateService.ParseVersion(tag));
    }

    [Fact]
    public void GetAssetSuffixes_OrdersPlatformArchiveFirst()
    {
        Assert.EndsWith(".tar.gz", UpdateService.GetAssetSuffixes("linux-x64")[0], StringComparison.Ordinal);
        Assert.EndsWith(".zip", UpdateService.GetAssetSuffixes("win-arm64")[0], StringComparison.Ordinal);
    }

    [Fact]
    public void RuntimeIdentifier_MatchesSupportedPlatformPattern()
    {
        Assert.Matches("^(win|linux|osx)-(x64|arm64|x86)$", AppConfig.RuntimeIdentifier);
    }
}
