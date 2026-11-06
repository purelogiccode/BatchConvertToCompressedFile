using System.Net;
using BatchConvertToCompressedFile.Models;
using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class UpdateServiceTests
{
    [Fact]
    public void GitHubAsset_ExposesReleaseAssetProperties()
    {
        var asset = new GitHubAsset(
            "BatchConvertToCompressedFile_2.0.0_win-x64.zip",
            "https://example.test/win-x64.zip");

        Assert.Equal("BatchConvertToCompressedFile_2.0.0_win-x64.zip", asset.Name);
        Assert.Equal("https://example.test/win-x64.zip", asset.BrowserDownloadUrl);
    }

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

    [Theory]
    [InlineData("win-x64", "https://example.test/BatchConvertToCompressedFile_99.1.2_win-x64.zip")]
    [InlineData("win-arm64", "https://example.test/BatchConvertToCompressedFile_99.1.2_win-arm64.zip")]
    [InlineData("linux-x64", "https://example.test/BatchConvertToCompressedFile_99.1.2_linux-x64.tar.gz")]
    [InlineData("linux-arm64", "https://example.test/BatchConvertToCompressedFile_99.1.2_linux-arm64.tar.gz")]
    [InlineData("osx-x64", "https://example.test/BatchConvertToCompressedFile_99.1.2_osx-x64.tar.gz")]
    [InlineData("osx-arm64", "https://example.test/BatchConvertToCompressedFile_99.1.2_osx-arm64.tar.gz")]
    public async Task CheckForUpdateAsync_AllReleaseAssetNames_SelectCorrectBundle(
        string runtimeIdentifier,
        string expectedUrl)
    {
        const string allAssetsJson = """
            {
              "tag_name": "v99.1.2",
              "html_url": "https://github.com/purelogiccode/BatchConvertToCompressedFile/releases/tag/v99.1.2",
              "assets": [
                { "name": "BatchConvertToCompressedFile_99.1.2_win-x64.zip", "browser_download_url": "https://example.test/BatchConvertToCompressedFile_99.1.2_win-x64.zip" },
                { "name": "BatchConvertToCompressedFile_99.1.2_win-arm64.zip", "browser_download_url": "https://example.test/BatchConvertToCompressedFile_99.1.2_win-arm64.zip" },
                { "name": "BatchConvertToCompressedFile_99.1.2_linux-x64.tar.gz", "browser_download_url": "https://example.test/BatchConvertToCompressedFile_99.1.2_linux-x64.tar.gz" },
                { "name": "BatchConvertToCompressedFile_99.1.2_linux-arm64.tar.gz", "browser_download_url": "https://example.test/BatchConvertToCompressedFile_99.1.2_linux-arm64.tar.gz" },
                { "name": "BatchConvertToCompressedFile_99.1.2_osx-x64.tar.gz", "browser_download_url": "https://example.test/BatchConvertToCompressedFile_99.1.2_osx-x64.tar.gz" },
                { "name": "BatchConvertToCompressedFile_99.1.2_osx-arm64.tar.gz", "browser_download_url": "https://example.test/BatchConvertToCompressedFile_99.1.2_osx-arm64.tar.gz" }
              ]
            }
            """;

        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Json(HttpStatusCode.OK, allAssetsJson)));
        using var client = new HttpClient(handler);
        using var service = new UpdateService(
            "https://api.github.test/latest", client, new Version(1, 0, 0, 0), runtimeIdentifier);

        var result = await service.CheckForUpdateAsync();

        Assert.True(result.IsNewVersionAvailable);
        Assert.Equal(expectedUrl, result.DownloadUrl);
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
    public async Task CheckForUpdateAsync_CallerCancellation_Throws()
    {
        var handler = new StubHttpMessageHandler(async (_, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token);
            return StubHttpMessageHandler.Empty(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        using var service = new UpdateService(
            "https://api.github.test/latest", client, new Version(1, 0, 0, 0), "win-x64");
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.CheckForUpdateAsync(cancellation.Token));
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
