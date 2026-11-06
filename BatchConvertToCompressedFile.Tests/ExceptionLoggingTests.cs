using System.Text.Json;
using BatchConvertToCompressedFile.Logging;

namespace BatchConvertToCompressedFile.Tests;

public sealed class ExceptionLoggingTests
{
    [Theory]
    [MemberData(nameof(ExpectedExceptions))]
    public void IsExpected_RecognizesEnvironmentConditions(Exception exception)
    {
        Assert.True(ExceptionLogging.IsExpected(exception));
    }

    [Fact]
    public void IsExpected_DoesNotHideProgrammingErrors()
    {
        Assert.False(ExceptionLogging.IsExpected(new InvalidOperationException("bug")));
    }

    [Fact]
    public void LogHandled_DoesNotThrowForExpectedOrUnexpectedExceptions()
    {
        ExceptionLogging.LogHandled(new IOException("locked"), "Expected {Value}", 1);
        ExceptionLogging.LogHandled(new InvalidOperationException("bug"), "Unexpected {Value}", 2);
    }

    public static TheoryData<Exception> ExpectedExceptions => new()
    {
        new IOException("io"),
        new UnauthorizedAccessException("denied"),
        new InvalidDataException("corrupt"),
        new HttpRequestException("offline"),
        new JsonException("bad json")
    };
}
