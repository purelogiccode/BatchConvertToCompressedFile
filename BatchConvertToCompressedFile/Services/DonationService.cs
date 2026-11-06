using System.Net;
using System.Net.Http.Json;
using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Models;

namespace BatchConvertToCompressedFile.Services;

/// <summary>Verifies donations through the Pure Logic Code donation API.</summary>
internal sealed class DonationService : IDonationService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationSlug;
    private readonly bool _ownsClient;

    /// <summary>Initializes a new donation service.</summary>
    /// <param name="apiUrl">Donation verification endpoint.</param>
    /// <param name="apiKey">API key sent in the X-API-KEY header.</param>
    /// <param name="applicationSlug">Application slug registered by the donation service.</param>
    /// <param name="httpClient">Optional HTTP client for tests.</param>
    public DonationService(
        string apiUrl,
        string apiKey,
        string applicationSlug,
        HttpClient? httpClient = null)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationSlug = applicationSlug;
        _httpClient = httpClient ?? new HttpClient();
        _ownsClient = httpClient is null;
    }

    /// <inheritdoc />
    public async Task<DonationVerificationResult> VerifyDonationAsync(
        string email,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new DonationVerificationResult(false, null, "Enter the email address used for the donation.");
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Headers.TryAddWithoutValidation("X-API-KEY", _apiKey);
            request.Content = JsonContent.Create(
                new DonationVerificationRequest(email.Trim(), _applicationSlug));

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));

            using var response = await _httpClient.SendAsync(request, timeoutCts.Token);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                Log.Information(
                    "Donation verification is not configured for application slug {ApplicationSlug}",
                    _applicationSlug);
                return new DonationVerificationResult(
                    false, null,
                    "Donation verification is not available for this application yet. Please try again later.");
            }

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                Log.Warning("Donation verification was rejected because the API key is invalid");
                return new DonationVerificationResult(
                    false, null, "The donation service rejected the request. Please update the application.");
            }

            if (!response.IsSuccessStatusCode)
            {
                Log.Information(
                    "Donation verification returned {StatusCode}", (int)response.StatusCode);
                return new DonationVerificationResult(
                    false, null, "The donation service is temporarily unavailable. Please try again later.");
            }

            var payload = await response.Content.ReadFromJsonAsync<DonationVerificationResponse>(
                cancellationToken: cancellationToken);

            if (payload is null)
            {
                return new DonationVerificationResult(
                    false, null, "The donation service returned an empty response.");
            }

            return new DonationVerificationResult(
                true,
                payload,
                payload.Donated
                    ? "Thank you! Your donation has been verified."
                    : "No completed donation was found for this email address.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Information("Donation verification timed out");
            return new DonationVerificationResult(
                false, null, "The donation service did not respond in time. Please try again later.");
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            Log.Information("Donation verification failed: {Message}", ex.Message);
            return new DonationVerificationResult(
                false, null, "Could not reach the donation service. Check your internet connection and try again.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error during donation verification");
            return new DonationVerificationResult(
                false, null, "An unexpected error occurred while verifying the donation.");
        }
    }

    /// <inheritdoc />
    public string BuildDonationUrl(string? email)
    {
        var url = AppConfig.DonateUrl;
        if (string.IsNullOrWhiteSpace(email)) return url;

        return $"{url}&email={Uri.EscapeDataString(email.Trim())}";
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsClient) _httpClient.Dispose();
    }
}
