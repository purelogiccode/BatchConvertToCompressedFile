using System.Globalization;
using System.Text.RegularExpressions;

namespace BatchConvertToCompressedFile.Services;

/// <summary>File-system path helpers shared by the application and its tests.</summary>
internal static class PathUtilities
{
    /// <summary>Replaces characters that are invalid in a file name with an underscore.</summary>
    /// <param name="name">Candidate file name (without extension).</param>
    /// <returns>A file name that is safe on the current platform.</returns>
    public static string SanitizeFileName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        // Always strip the Windows-invalid set, even on Linux/macOS, so generated archive names
        // stay portable across all supported platforms.
        var invalidCharacterSet = new HashSet<char>(Path.GetInvalidFileNameChars())
        {
            '<', '>', ':', '"', '/', '\\', '|', '?', '*'
        };

        for (var character = 0; character < 32; character++)
        {
            invalidCharacterSet.Add((char)character);
        }

        var invalidChars = Regex.Escape(new string(invalidCharacterSet.ToArray()));
        var invalidPattern = string.Format(CultureInfo.InvariantCulture, @"([{0}]*\.+$)|([{0}]+)", invalidChars);
        var sanitized = Regex.Replace(name, invalidPattern, "_");

        return string.IsNullOrWhiteSpace(sanitized) ? "_" : sanitized;
    }

    /// <summary>Gets the per-user data folder used for logs and settings.</summary>
    public static string GetApplicationDataFolder()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(root))
        {
            root = Path.Combine(Path.GetTempPath(), AppConfig.ApplicationName);
        }
        else
        {
            root = Path.Combine(root, AppConfig.ApplicationName);
        }

        return root;
    }

    /// <summary>Gets the folder that stores rolling log files.</summary>
    public static string GetLogFolder() => Path.Combine(GetApplicationDataFolder(), "logs");

    /// <summary>Gets the full path of the persisted donation state file.</summary>
    public static string GetDonationStateFilePath() =>
        Path.Combine(GetApplicationDataFolder(), "donation.json");

    /// <summary>Returns whether two folder paths point to the same location.</summary>
    /// <param name="left">First path.</param>
    /// <param name="right">Second path.</param>
    /// <returns><see langword="true"/> when the normalized paths are equal.</returns>
    public static bool AreSameFolder(string? left, string? right)
    {
        if (string.IsNullOrWhiteSpace(left) || string.IsNullOrWhiteSpace(right)) return false;

        try
        {
            var normalizedLeft = Path.TrimEndingDirectorySeparator(Path.GetFullPath(left));
            var normalizedRight = Path.TrimEndingDirectorySeparator(Path.GetFullPath(right));
            return string.Equals(normalizedLeft, normalizedRight, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Log.Debug(ex, "Could not normalize paths {Left} and {Right}", left, right);
            return string.Equals(left, right, StringComparison.OrdinalIgnoreCase);
        }
    }

    /// <summary>Gets the relative directory of a file below a base folder, or an empty string.</summary>
    /// <param name="baseFolder">Base input folder.</param>
    /// <param name="filePath">File being moved.</param>
    /// <returns>A relative directory suitable for <see cref="Path.Combine(string, string)"/>.</returns>
    public static string GetRelativeDirectory(string baseFolder, string filePath)
    {
        var directory = Path.GetDirectoryName(filePath);
        if (string.IsNullOrEmpty(directory) || AreSameFolder(directory, baseFolder)) return string.Empty;

        return Path.GetRelativePath(baseFolder, directory);
    }
}
