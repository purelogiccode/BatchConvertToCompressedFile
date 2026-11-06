using BatchConvertToCompressedFile.Logging;

namespace BatchConvertToCompressedFile.Tests;

public sealed class BugReportFormatterTests
{
    [Fact]
    public void Build_IncludesEveryRequiredEnvironmentField()
    {
        var report = BugReportFormatter.Build("Something failed", new InvalidOperationException("boom"));

        Assert.Contains("=== Environment Details ===", report);
        Assert.Contains("Date:", report);
        Assert.Contains("Application Name:", report);
        Assert.Contains("Application Version:", report);
        Assert.Contains("OS Version:", report);
        Assert.Contains("Architecture:", report);
        Assert.Contains("Bitness:", report);
        Assert.Contains("Version:", report);
        Assert.Contains("Processor Count:", report);
        Assert.Contains("Base Directory:", report);
        Assert.Contains("Temp Path:", report);
    }

    [Fact]
    public void Build_IncludesErrorAndExceptionSections()
    {
        var report = BugReportFormatter.Build("Something failed", new InvalidOperationException("boom"));

        Assert.Contains("=== Error Details ===", report);
        Assert.Contains("Error message: Something failed", report);
        Assert.Contains("=== Exception Details ===", report);
        Assert.Contains("Type: System.InvalidOperationException", report);
        Assert.Contains("Message: boom", report);
        Assert.Contains("Source:", report);
        Assert.Contains("StackTrace:", report);
    }

    [Fact]
    public void Build_WithoutException_UsesNonePlaceholders()
    {
        var report = BugReportFormatter.Build("Only a message", null);

        Assert.Contains("Type: None", report);
        Assert.Contains("Message: None", report);
        Assert.Contains("Source: None", report);
        Assert.Contains("StackTrace: None", report);
    }

    [Fact]
    public void Build_IncludesInnerExceptionChain()
    {
        var exception = new InvalidOperationException("outer", new ArgumentException("inner"));

        var report = BugReportFormatter.Build("failure", exception);

        Assert.Contains("--- Inner Exception ---", report);
        Assert.Contains("Type: System.ArgumentException", report);
        Assert.Contains("Message: inner", report);
    }

    [Fact]
    public void PlatformVersionLabel_IsPlatformSpecific()
    {
        var label = OsEnvironmentInfo.GetPlatformVersionLabel();

        Assert.Contains(label, new[]
        {
            "Windows Version", "Linux Version", "MacOsX Version", "OS Version"
        });
        Assert.False(string.IsNullOrWhiteSpace(OsEnvironmentInfo.GetOsVersionName()));
    }
}
