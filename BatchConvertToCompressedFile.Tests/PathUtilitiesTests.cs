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
    public void SanitizeFileName_ReplacesInvalidCharacters(string input, string expected)
    {
        Assert.Equal(expected, PathUtilities.SanitizeFileName(input));
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
    public void ApplicationDataFolder_IsUnderLocalApplicationDataOrTemp()
    {
        var folder = PathUtilities.GetApplicationDataFolder();
        Assert.Contains(AppConfig.ApplicationName, folder, StringComparison.OrdinalIgnoreCase);
        Assert.StartsWith(folder, PathUtilities.GetLogFolder(), StringComparison.OrdinalIgnoreCase);
    }
}
