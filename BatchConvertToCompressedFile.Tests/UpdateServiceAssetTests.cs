using System.Net;
using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class UpdateServiceAssetTests
{
    [Fact]
    public async Task CheckForUpdateAsync_NoMatchingAsset_StillOffersReleasePage()
    {
        const string json = """
            {
              "tag_name": "v9.9.9",
              "html_url": "https://github.com/purelogiccode/BatchConvertToCompressedFile/releases/tag/v9.9.9",
              "assets": [
                { "name": "some-other-file.txt", "browser_download_url": "https://example.test/file.txt" }
              ]
            }
            """;

        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Json(HttpStatusCode.OK, json)));
        using var client = new HttpClient(handler);
        using var service = new UpdateService(
            "https://api.github.test/latest", client, new Version(1, 0, 0, 0), "linux-arm64");

        var result = await service.CheckForUpdateAsync();

        Assert.True(result.IsNewVersionAvailable);
        Assert.Equal("9.9.9", result.LatestVersion);
        Assert.Null(result.DownloadUrl);
        Assert.Contains("releases/tag", result.ReleaseUrl);
    }
}
