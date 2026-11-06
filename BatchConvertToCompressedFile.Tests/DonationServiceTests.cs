using System.Net;
using BatchConvertToCompressedFile.Models;
using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class DonationServiceTests
{
    [Fact]
    public void DonationVerificationRequest_ExposesPayloadProperties()
    {
        var request = new DonationVerificationRequest("donor@example.com", "slug");

        Assert.Equal("donor@example.com", request.Email);
        Assert.Equal("slug", request.ApplicationSlug);
    }
    [Fact]
    public async Task VerifyDonationAsync_Donated_ReturnsSuccess()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Json(
                HttpStatusCode.OK,
                """{"donated":true,"applicationSlug":"batch-convert-to-compressed-file","totalAmount":10.5,"lastDonationAtUtc":"2026-01-01T00:00:00Z"}""")));
        using var client = new HttpClient(handler);
        using var service = new DonationService(
            "https://example.test/verify", "secret", "batch-convert-to-compressed-file", client);

        var result = await service.VerifyDonationAsync("Donor@Example.com");

        Assert.True(result.Succeeded);
        Assert.True(result.Donated);
        Assert.Contains("Thank you", result.Message);
        var request = Assert.Single(handler.Requests);
        Assert.Equal("secret", request.ApiKey);
        Assert.Contains("donor@example.com", request.Body, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("batch-convert-to-compressed-file", request.Body);
    }

    [Fact]
    public async Task VerifyDonationAsync_NotDonated_ReturnsSuccessWithoutDonation()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Json(
                HttpStatusCode.OK,
                """{"donated":false,"applicationSlug":"batch-convert-to-compressed-file","totalAmount":0}""")));
        using var client = new HttpClient(handler);
        using var service = new DonationService(
            "https://example.test/verify", "secret", "batch-convert-to-compressed-file", client);

        var result = await service.VerifyDonationAsync("nobody@example.com");

        Assert.True(result.Succeeded);
        Assert.False(result.Donated);
        Assert.Contains("No completed donation", result.Message);
    }

    [Fact]
    public async Task VerifyDonationAsync_NotFound_ReturnsFriendlyMessage()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.NotFound)));
        using var client = new HttpClient(handler);
        using var service = new DonationService(
            "https://example.test/verify", "secret", "batch-convert-to-compressed-file", client);

        var result = await service.VerifyDonationAsync("nobody@example.com");

        Assert.False(result.Succeeded);
        Assert.False(result.Donated);
        Assert.Contains("not available", result.Message);
    }

    [Fact]
    public async Task VerifyDonationAsync_Unauthorized_ReturnsFriendlyMessage()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.Unauthorized)));
        using var client = new HttpClient(handler);
        using var service = new DonationService(
            "https://example.test/verify", "wrong", "batch-convert-to-compressed-file", client);

        var result = await service.VerifyDonationAsync("nobody@example.com");

        Assert.False(result.Succeeded);
        Assert.Contains("rejected", result.Message);
    }

    [Fact]
    public async Task VerifyDonationAsync_ServerError_ReturnsFriendlyMessage()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.InternalServerError)));
        using var client = new HttpClient(handler);
        using var service = new DonationService(
            "https://example.test/verify", "secret", "batch-convert-to-compressed-file", client);

        var result = await service.VerifyDonationAsync("nobody@example.com");

        Assert.False(result.Succeeded);
        Assert.Contains("temporarily unavailable", result.Message);
    }

    [Fact]
    public async Task VerifyDonationAsync_InvalidJson_ReturnsFriendlyMessage()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Json(HttpStatusCode.OK, "{not-json")));
        using var client = new HttpClient(handler);
        using var service = new DonationService(
            "https://example.test/verify", "secret", "batch-convert-to-compressed-file", client);

        var result = await service.VerifyDonationAsync("nobody@example.com");

        Assert.False(result.Succeeded);
        Assert.Contains("unexpected response", result.Message);
    }

    [Fact]
    public async Task VerifyDonationAsync_NetworkError_ReturnsFriendlyMessage()
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            throw new HttpRequestException("offline"));
        using var client = new HttpClient(handler);
        using var service = new DonationService(
            "https://example.test/verify", "secret", "batch-convert-to-compressed-file", client);

        var result = await service.VerifyDonationAsync("nobody@example.com");

        Assert.False(result.Succeeded);
        Assert.Contains("Could not reach", result.Message);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task VerifyDonationAsync_EmptyEmail_DoesNotCallApi(string email)
    {
        var handler = new StubHttpMessageHandler((_, _) =>
            Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.OK)));
        using var client = new HttpClient(handler);
        using var service = new DonationService(
            "https://example.test/verify", "secret", "batch-convert-to-compressed-file", client);

        var result = await service.VerifyDonationAsync(email);

        Assert.False(result.Succeeded);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public void BuildDonationUrl_EncodesEmail()
    {
        using var service = new DonationService(
            "https://example.test/verify", "secret", "slug", new HttpClient(new StubHttpMessageHandler(
                (_, _) => Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.OK)))));

        var url = service.BuildDonationUrl("donor+tag@example.com");

        Assert.Contains("app=batch-convert-to-compressed-file", url);
        Assert.Contains("email=donor%2Btag%40example.com", url);
    }

    [Fact]
    public void BuildDonationUrl_WithoutEmail_ReturnsBaseUrl()
    {
        using var service = new DonationService(
            "https://example.test/verify", "secret", "slug", new HttpClient(new StubHttpMessageHandler(
                (_, _) => Task.FromResult(StubHttpMessageHandler.Empty(HttpStatusCode.OK)))));

        Assert.Equal(AppConfig.DonateUrl, service.BuildDonationUrl(null));
    }
}
