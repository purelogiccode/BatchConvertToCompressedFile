using System.Text.RegularExpressions;

namespace BatchConvertToCompressedFile.Services;

/// <summary>File-system path helpers shared by the application and its tests.</summary>
internal static class PathUtilities
{
    private const int MaxFileNameLength = 180;

    private static readonly HashSet<string> ReservedWindowsNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private static readonly Regex InvalidFileNamePattern = CreateInvalidFileNamePattern();

    /// <summary>Gets the comparison used for file-system paths on the current platform.</summary>
    private static StringComparison PathComparison { get; } =
        OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;

    /// <summary>Gets the comparer used for file-system paths on the current platform.</summary>
    public static StringComparer PathComparer { get; } =
        OperatingSystem.IsWindows() ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal;

    /// <summary>Replaces characters that are invalid in a file name with an underscore.</summary>
    /// <param name="name">Candidate file name (without extension).</param>
    /// <returns>A file name that is safe on the current platform and on Windows.</returns>
    public static string SanitizeFileName(string name)
    {
        ArgumentNullException.ThrowIfNull(name);

        var sanitized = InvalidFileNamePattern.Replace(name, "_").Trim();
        sanitized = sanitized.TrimEnd('.', ' ');

        if (sanitized.Length > MaxFileNameLength)
        {
            sanitized = sanitized[..MaxFileNameLength].TrimEnd('.', ' ');
        }

        if (string.IsNullOrWhiteSpace(sanitized))
        {
            return "_";
        }

        // Windows reserves device names even when an extension is appended (for example CON.zip).
        var stem = sanitized.Split('.', 2)[0];
        return ReservedWindowsNames.Contains(stem) ? $"_{sanitized}" : sanitized;
    }

    /// <summary>Gets the per-user data folder used for logs and settings.</summary>
    /// <returns>The application data folder path.</returns>
    public static string GetApplicationDataFolder()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return string.IsNullOrWhiteSpace(root)
            ? Path.Combine(Path.GetTempPath(), AppConfig.ApplicationName)
            : Path.Combine(root, AppConfig.ApplicationName);
    }

    /// <summary>Gets the folder that stores rolling log files.</summary>
    /// <returns>The log folder path.</returns>
    public static string GetLogFolder() => Path.Combine(GetApplicationDataFolder(), "logs");

    /// <summary>Gets the full path of the persisted donation state file.</summary>
    /// <returns>The donation state file path.</returns>
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
            return string.Equals(normalizedLeft, normalizedRight, PathComparison);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Log.Debug(ex, "Could not normalize paths {Left} and {Right}", left, right);
            return string.Equals(left, right, PathComparison);
        }
    }

    /// <summary>Returns whether a path is inside a base folder, including the base folder itself.</summary>
    /// <param name="baseFolder">Base folder path.</param>
    /// <param name="path">Candidate path.</param>
    /// <returns><see langword="true"/> when <paramref name="path"/> is contained by <paramref name="baseFolder"/>.</returns>
    public static bool IsSubPathOf(string baseFolder, string path)
    {
        if (string.IsNullOrWhiteSpace(baseFolder) || string.IsNullOrWhiteSpace(path)) return false;

        try
        {
            var normalizedBase = Path.TrimEndingDirectorySeparator(Path.GetFullPath(baseFolder));
            var normalizedPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(path));

            if (string.Equals(normalizedBase, normalizedPath, PathComparison)) return true;

            var prefix = normalizedBase.EndsWith(Path.DirectorySeparatorChar)
                ? normalizedBase
                : normalizedBase + Path.DirectorySeparatorChar;
            return normalizedPath.StartsWith(prefix, PathComparison);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Log.Debug(ex, "Could not compare paths {BaseFolder} and {Path}", baseFolder, path);
            return false;
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

        try
        {
            return Path.GetRelativePath(baseFolder, directory);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            Log.Debug(ex, "Could not build a relative path from {BaseFolder} to {FilePath}", baseFolder, filePath);
            return string.Empty;
        }
    }

    /// <summary>
    /// Reserves a unique output path for a file name, adding a numeric suffix when the name is
    /// already used by another file in the same batch.
    /// </summary>
    /// <param name="folder">Destination folder.</param>
    /// <param name="baseName">Sanitized file name without extension.</param>
    /// <param name="extension">File extension including the leading dot.</param>
    /// <param name="reservedPaths">Paths already reserved by the current batch.</param>
    /// <returns>A path that was added to <paramref name="reservedPaths"/>.</returns>
    public static string ReserveUniqueFilePath(
        string folder,
        string baseName,
        string extension,
        ISet<string> reservedPaths)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(folder);
        ArgumentException.ThrowIfNullOrWhiteSpace(baseName);
        ArgumentException.ThrowIfNullOrWhiteSpace(extension);
        ArgumentNullException.ThrowIfNull(reservedPaths);

        var candidate = Path.Combine(folder, baseName + extension);
        var suffix = 2;

        while (!reservedPaths.Add(candidate))
        {
            candidate = Path.Combine(folder, $"{baseName} ({suffix}){extension}");
            suffix++;
        }

        return candidate;
    }

    private static Regex CreateInvalidFileNamePattern()
    {
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
        return new Regex($"[{invalidChars}]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);
    }
}
