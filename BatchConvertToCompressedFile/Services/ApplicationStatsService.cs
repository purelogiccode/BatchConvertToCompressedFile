using System.Net.Http.Headers;
using System.Net.Http.Json;
using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Models;

namespace BatchConvertToCompressedFile.Services;

/// <summary>Records one anonymous application-launch statistic.</summary>
internal sealed class ApplicationStatsService : IApplicationStatsService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationId;
    private readonly string _version;
    private readonly bool _ownsClient;

    /// <summary>Initializes a new statistics service.</summary>
    /// <param name="apiUrl">Statistics endpoint.</param>
    /// <param name="apiKey">Bearer token.</param>
    /// <param name="applicationId">Application identifier.</param>
    /// <param name="version">Application version.</param>
    /// <param name="httpClient">Optional HTTP client for tests.</param>
    public ApplicationStatsService(
        string apiUrl,
        string apiKey,
        string applicationId,
        string version,
        HttpClient? httpClient = null)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationId = applicationId;
        _version = version;
        _httpClient = httpClient ?? new HttpClient();
        _ownsClient = httpClient is null;
    }

    /// <inheritdoc />
    public async Task<bool> RecordStartupAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = JsonContent.Create(new ApplicationStatsRequest(_applicationId, _version));

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(20));

            using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
            if (response.IsSuccessStatusCode)
            {
                Log.Information("Startup statistics recorded");
                return true;
            }

            // Rate limiting and WAF responses are expected external conditions.
            Log.Information("Statistics API returned {StatusCode}", (int)response.StatusCode);
            return false;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            Log.Information("Statistics API call timed out");
            return false;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            Log.Information("Statistics API call failed: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while recording startup statistics");
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsClient) _httpClient.Dispose();
    }
}
