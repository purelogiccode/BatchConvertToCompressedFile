using System.Net.Http.Json;
using BatchConvertToCompressedFile.Interfaces;

namespace BatchConvertToCompressedFile.Logging;

/// <summary>Posts bug reports to the Pure Logic Code bug report API.</summary>
internal sealed class BugReportService : IBugReportService, IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly string _apiUrl;
    private readonly string _apiKey;
    private readonly string _applicationName;
    private readonly bool _ownsClient;

    /// <summary>Initializes a new bug report service.</summary>
    /// <param name="apiUrl">Bug report endpoint.</param>
    /// <param name="apiKey">API key sent in the X-API-KEY header.</param>
    /// <param name="applicationName">Application name included in the payload.</param>
    /// <param name="httpClient">Optional HTTP client for tests.</param>
    public BugReportService(
        string apiUrl,
        string apiKey,
        string applicationName,
        HttpClient? httpClient = null)
    {
        _apiUrl = apiUrl;
        _apiKey = apiKey;
        _applicationName = applicationName;
        _httpClient = httpClient ?? new HttpClient();
        _ownsClient = httpClient is null;
    }

    /// <inheritdoc />
    public async Task<bool> SendBugReportAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _apiUrl);
            request.Headers.TryAddWithoutValidation("X-API-KEY", _apiKey);
            request.Content = JsonContent.Create(new
            {
                message,
                applicationName = _applicationName,
                version = AppConfig.ApplicationVersionString,
                environment = OsEnvironmentInfo.GetOsVersionName()
            });

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));

            using var response = await _httpClient.SendAsync(request, timeoutCts.Token);
            if (!response.IsSuccessStatusCode)
            {
                Log.Debug("Bug report API returned {StatusCode} {ReasonPhrase}",
                    (int)response.StatusCode, response.ReasonPhrase);
            }

            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException)
        {
            return false;
        }
        catch (Exception ex)
        {
            // Debug level is below Warning, so this can never re-enter the bug report sink.
            Log.Debug("Bug report submission failed: {Message}", ex.Message);
            return false;
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_ownsClient) _httpClient.Dispose();
    }
}
