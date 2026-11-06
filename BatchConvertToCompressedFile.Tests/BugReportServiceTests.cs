using System.Net;
using System.Text.Json;
using BatchConvertToCompressedFile.Logging;

namespace BatchConvertToCompressedFile.Tests;

public sealed class BugReportServiceTests
{
    [Fact]
    public async Task SendBugReportAsync_PostsReportWithApiKey()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.OK)));
        using var client = new HttpClient(handler);
        using var service = new BugReportService(
            "https://example.test/bugreport", "api-key", "TestApp", client);

        var sent = await service.SendBugReportAsync("=== Environment Details ===\nTest");

        Assert.True(sent);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal("api-key", request.ApiKey);

        using var document = JsonDocument.Parse(request.Body!);
        Assert.Equal("TestApp", document.RootElement.GetProperty("applicationName").GetString());
        Assert.Contains("Environment Details", document.RootElement.GetProperty("message").GetString());
    }

    [Fact]
    public async Task SendBugReportAsync_ServerError_ReturnsFalse()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.InternalServerError)));
        using var client = new HttpClient(handler);
        using var service = new BugReportService(
            "https://example.test/bugreport", "api-key", "TestApp", client);

        Assert.False(await service.SendBugReportAsync("report"));
    }

    [Fact]
    public async Task SendBugReportAsync_NetworkError_ReturnsFalseWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler((_, _) => throw new HttpRequestException("offline"));
        using var client = new HttpClient(handler);
        using var service = new BugReportService(
            "https://example.test/bugreport", "api-key", "TestApp", client);

        Assert.False(await service.SendBugReportAsync("report"));
    }
}
