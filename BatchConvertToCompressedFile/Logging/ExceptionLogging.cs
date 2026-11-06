using System.Text.Json;
using SharpCompress.Common;

namespace BatchConvertToCompressedFile.Logging;

/// <summary>
/// Classifies handled exceptions so expected user conditions never produce noisy bug reports.
/// </summary>
internal static class ExceptionLogging
{
    /// <summary>Determines whether an exception represents an expected user or environment condition.</summary>
    /// <param name="exception">Exception to classify.</param>
    /// <returns><see langword="true"/> for expected conditions; otherwise <see langword="false"/>.</returns>
    public static bool IsExpected(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception is IOException
            or UnauthorizedAccessException
            or NotSupportedException
            or InvalidDataException
            or System.Security.Cryptography.CryptographicException
            or HttpRequestException
            or JsonException
            or SharpCompressException;
    }

    /// <summary>
    /// Logs a handled exception at Information for expected conditions or Error for unexpected failures.
    /// </summary>
    /// <param name="exception">Exception to log.</param>
    /// <param name="messageTemplate">Serilog message template.</param>
    /// <param name="propertyValues">Template property values.</param>
    public static void LogHandled(
        Exception exception,
        string messageTemplate,
        params object?[] propertyValues)
    {
        ArgumentNullException.ThrowIfNull(exception);
        ArgumentException.ThrowIfNullOrWhiteSpace(messageTemplate);

        if (IsExpected(exception))
        {
            Log.Information(exception, messageTemplate, propertyValues);
        }
        else
        {
            Log.Error(exception, messageTemplate, propertyValues);
        }
    }
}
