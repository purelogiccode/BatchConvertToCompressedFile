using System.Net;
using System.Text.Json;
using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class ApplicationStatsServiceTests
{
    [Fact]
    public async Task RecordStartupAsync_SendsBearerTokenAndPayload()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Json(
                HttpStatusCode.OK, """{"message":"Stats recorded successfully"}""")));
        using var client = new HttpClient(handler);
        using var service = new ApplicationStatsService(
            "https://example.test/stats", "stats-key", "batchconverttocompressedfile", "2.0.0", client);

        var recorded = await service.RecordStartupAsync();

        Assert.True(recorded);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("Bearer stats-key", request.Authorization);

        using var document = JsonDocument.Parse(request.Body!);
        Assert.Equal("batchconverttocompressedfile", document.RootElement.GetProperty("applicationId").GetString());
        Assert.Equal("2.0.0", document.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public async Task RecordStartupAsync_RateLimited_ReturnsFalseWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty((HttpStatusCode)429)));
        using var client = new HttpClient(handler);
        using var service = new ApplicationStatsService(
            "https://example.test/stats", "stats-key", "app", "1.0.0", client);

        Assert.False(await service.RecordStartupAsync());
    }

    [Fact]
    public async Task RecordStartupAsync_NetworkFailure_ReturnsFalseWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new HttpRequestException("offline"));
        using var client = new HttpClient(handler);
        using var service = new ApplicationStatsService(
            "https://example.test/stats", "stats-key", "app", "1.0.0", client);

        Assert.False(await service.RecordStartupAsync());
    }

    [Fact]
    public async Task RecordStartupAsync_Cancellation_Throws()
    {
        var handler = new StubHttpMessageHandler(async (_, token) =>
        {
            await Task.Delay(TimeSpan.FromSeconds(5), token);
            return StubHttpMessageHandler.Empty(HttpStatusCode.OK);
        });
        using var client = new HttpClient(handler);
        using var service = new ApplicationStatsService(
            "https://example.test/stats", "stats-key", "app", "1.0.0", client);
        using var cancellation = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
            service.RecordStartupAsync(cancellation.Token));
    }
}
