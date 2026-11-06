using System.Reflection;

namespace BatchConvertToCompressedFile;

/// <summary>
/// Central application configuration: identity, endpoints, and environment-overridable API keys.
/// </summary>
internal static class AppConfig
{
    /// <summary>Technical application name used in log paths and API payloads.</summary>
    public const string ApplicationName = "BatchConvertToCompressedFile";

    /// <summary>Human-friendly product name shown in the UI.</summary>
    public const string DisplayName = "Batch Convert to Compressed File";

    /// <summary>Short product description shown in the About window.</summary>
    public const string Description =
        "Compress files into individual .7z or .zip archives and verify the integrity of existing archives.";

    /// <summary>Public project website.</summary>
    public const string WebsiteUrl = "https://www.purelogiccode.com";

    /// <summary>GitHub repository URL.</summary>
    public const string RepositoryUrl = "https://github.com/purelogiccode/BatchConvertToCompressedFile";

    /// <summary>GitHub latest-release API endpoint.</summary>
    public const string UpdateApiUrl =
        "https://api.github.com/repos/purelogiccode/BatchConvertToCompressedFile/releases/latest";

    /// <summary>GitHub releases page used when no direct asset URL is available.</summary>
    public const string ReleasesUrl = "https://github.com/purelogiccode/BatchConvertToCompressedFile/releases";

    /// <summary>Donation page for this application.</summary>
    public const string DonateUrl =
        "https://www.purelogiccode.com/Donate?app=batch-convert-to-compressed-file";

    /// <summary>Donation verification API endpoint.</summary>
    public const string DonationApiUrl = "https://www.purelogiccode.com/api/donations/verify";

    /// <summary>Application slug used by the donation service.</summary>
    public const string DonationApplicationSlug = "batch-convert-to-compressed-file";

    /// <summary>Application usage statistics endpoint.</summary>
    public const string StatsApiUrl = "https://www.purelogiccode.com/ApplicationStats/stats";

    /// <summary>Application identifier used by the statistics service.</summary>
    public const string StatsApplicationId = "batchconverttocompressedfile";

    /// <summary>Bug report submission endpoint.</summary>
    public const string BugReportApiUrl = "https://www.purelogiccode.com/bugreport/api/send-bug-report";

    /// <summary>Default bug report API key. Override with the BUGREPORT_API_KEY environment variable.</summary>
    private const string DefaultBugReportApiKey =
        "hjh7yu6t56tyr540o9u8767676r5674534453235264c75b6t7ggghgg76trf564e";

    /// <summary>Bug report API key, optionally overridden by an environment variable.</summary>
    public static string BugReportApiKey => GetKey("BUGREPORT_API_KEY", DefaultBugReportApiKey);

    /// <summary>Donation API key, optionally overridden by an environment variable.</summary>
    public static string DonationApiKey => GetKey("DONATION_API_KEY", DefaultBugReportApiKey);

    /// <summary>Statistics API key, optionally overridden by an environment variable.</summary>
    public static string StatsApiKey => GetKey("APP_STATS_API_KEY", DefaultBugReportApiKey);

    /// <summary>Gets the running application version.</summary>
    public static Version ApplicationVersion =>
        Assembly.GetExecutingAssembly().GetName().Version ?? new Version(0, 0, 0, 0);

    /// <summary>Gets the running application version as a display string.</summary>
    public static string ApplicationVersionString => ApplicationVersion.ToString(3);

    /// <summary>Gets the current runtime identifier in the form used by release asset names.</summary>
    public static string RuntimeIdentifier
    {
        get
        {
            var architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture switch
            {
                System.Runtime.InteropServices.Architecture.Arm64 => "arm64",
                System.Runtime.InteropServices.Architecture.X64 => "x64",
                System.Runtime.InteropServices.Architecture.X86 => "x86",
                var other => other.ToString().ToLowerInvariant()
            };

            if (OperatingSystem.IsWindows()) return $"win-{architecture}";
            if (OperatingSystem.IsMacOS()) return $"osx-{architecture}";
            return $"linux-{architecture}";
        }
    }

    private static string GetKey(string environmentVariable, string fallback)
    {
        var value = Environment.GetEnvironmentVariable(environmentVariable);
        return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
    }
}
