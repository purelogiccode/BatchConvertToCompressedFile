namespace BatchConvertToCompressedFile.Interfaces;

/// <summary>Sends a formatted bug report to the bug report API.</summary>
internal interface IBugReportService
{
    /// <summary>Posts a formatted report to the bug report API.</summary>
    /// <param name="message">Complete report text.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the API accepted the report.</returns>
    Task<bool> SendBugReportAsync(string message, CancellationToken cancellationToken = default);
}
