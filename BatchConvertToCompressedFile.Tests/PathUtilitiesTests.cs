using BatchConvertToCompressedFile.Services;

namespace BatchConvertToCompressedFile.Tests;

public sealed class PathUtilitiesTests
{
    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("a/b", "a_b")]
    [InlineData("a\\b", "a_b")]
    [InlineData("a:b*c?d", "a_b_c_d")]
    [InlineData("...", "_")]
    [InlineData("   ", "_")]
    [InlineData("name.", "name")]
    [InlineData("name ", "name")]
    [InlineData("CON", "_CON")]
    [InlineData("con.txt", "_con.txt")]
    [InlineData("LPT9", "_LPT9")]
    public void SanitizeFileName_ReplacesInvalidCharacters(string input, string expected)
    {
        Assert.Equal(expected, PathUtilities.SanitizeFileName(input));
    }

    [Fact]
    public void SanitizeFileName_TruncatesVeryLongNames()
    {
        var result = PathUtilities.SanitizeFileName(new string('a', 500));

        Assert.Equal(180, result.Length);
    }

    [Fact]
    public void AreSameFolder_NormalizesTrailingSeparators()
    {
        var folder = Path.GetTempPath();
        Assert.True(PathUtilities.AreSameFolder(folder, folder + Path.DirectorySeparatorChar));
    }

    [Fact]
    public void AreSameFolder_DifferentFolders_ReturnsFalse()
    {
        Assert.False(PathUtilities.AreSameFolder(Path.GetTempPath(), AppContext.BaseDirectory));
    }

    [Theory]
    [InlineData(null, null)]
    [InlineData("", "something")]
    [InlineData("something", "")]
    public void AreSameFolder_EmptyValues_ReturnsFalse(string? left, string? right)
    {
        Assert.False(PathUtilities.AreSameFolder(left, right));
    }

    [Fact]
    public void GetRelativeDirectory_ReturnsRelativeSubfolder()
    {
        using var temp = new TempDirectory();
        var baseFolder = temp.Path;
        var file = temp.CreateFile(Path.Combine("a", "b", "file.zip"));

        Assert.Equal(Path.Combine("a", "b"), PathUtilities.GetRelativeDirectory(baseFolder, file));
    }

    [Fact]
    public void GetRelativeDirectory_FileInRoot_ReturnsEmpty()
    {
        using var temp = new TempDirectory();
        var file = temp.CreateFile("file.zip");

        Assert.Equal(string.Empty, PathUtilities.GetRelativeDirectory(temp.Path, file));
    }

    [Fact]
    public void AreSameFolder_UsesPlatformCaseSensitivity()
    {
        var folder = Path.Combine(Path.GetTempPath(), "BatchConvertCaseTest");
        var upper = folder.ToUpperInvariant();

        if (OperatingSystem.IsWindows())
        {
            Assert.True(PathUtilities.AreSameFolder(folder, upper));
        }
        else
        {
            Assert.False(PathUtilities.AreSameFolder(folder, upper));
        }
    }

    [Fact]
    public void IsSubPathOf_DetectsNestedAndEscapingPaths()
    {
        using var temp = new TempDirectory();
        var nested = Path.Combine(temp.Path, "nested", "file.zip");
        var sibling = Path.Combine(Path.GetDirectoryName(temp.Path)!, "sibling", "file.zip");

        Assert.True(PathUtilities.IsSubPathOf(temp.Path, temp.Path));
        Assert.True(PathUtilities.IsSubPathOf(temp.Path, nested));
        Assert.False(PathUtilities.IsSubPathOf(temp.Path, sibling));
        Assert.False(PathUtilities.IsSubPathOf(temp.Path, Path.Combine(temp.Path, "..", "outside.zip")));

        var root = Path.GetPathRoot(temp.Path);
        Assert.NotNull(root);
        Assert.True(PathUtilities.IsSubPathOf(root, temp.Path));
    }

    [Fact]
    public void ReserveUniqueFilePath_AddsSuffixForBatchCollisions()
    {
        using var temp = new TempDirectory();
        var reserved = new HashSet<string>(PathUtilities.PathComparer);

        var first = PathUtilities.ReserveUniqueFilePath(temp.Path, "same", ".zip", reserved);
        var second = PathUtilities.ReserveUniqueFilePath(temp.Path, "same", ".zip", reserved);

        Assert.Equal(Path.Combine(temp.Path, "same.zip"), first);
        Assert.Equal(Path.Combine(temp.Path, "same (2).zip"), second);
    }

    [Fact]
    public void ApplicationDataFolder_IsUnderLocalApplicationDataOrTemp()
    {
        var folder = PathUtilities.GetApplicationDataFolder();
        Assert.Contains(AppConfig.ApplicationName, folder, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(folder, PathUtilities.GetLogFolder(), StringComparison.OrdinalIgnoreCase);
    }
}
