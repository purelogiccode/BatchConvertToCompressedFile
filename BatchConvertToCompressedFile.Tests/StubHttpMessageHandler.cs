using System.Net;
using System.Text;

namespace BatchConvertToCompressedFile.Tests;

/// <summary>HTTP handler that records requests and returns a scripted response.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> _handler;

    public StubHttpMessageHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
    {
        _handler = handler;
    }

    public List<(HttpMethod Method, Uri? Uri, string? Body, string? ApiKey, string? Authorization)> Requests { get; } = [];

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var body = request.Content is null
            ? null
            : await request.Content.ReadAsStringAsync(cancellationToken);

        request.Headers.TryGetValues("X-API-KEY", out var apiKeys);
        Requests.Add((
            request.Method,
            request.RequestUri,
            body,
            apiKeys?.FirstOrDefault(),
            request.Headers.Authorization?.ToString()));

        return await _handler(request, cancellationToken);
    }

    public static HttpResponseMessage Json(HttpStatusCode statusCode, string json) =>
        new(statusCode)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };

    public static HttpResponseMessage Empty(HttpStatusCode statusCode) => new(statusCode);
}
