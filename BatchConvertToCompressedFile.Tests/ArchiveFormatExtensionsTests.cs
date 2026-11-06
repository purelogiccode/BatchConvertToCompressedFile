using BatchConvertToCompressedFile.Models;

namespace BatchConvertToCompressedFile.Tests;

public sealed class ArchiveFormatExtensionsTests
{
    [Theory]
    [InlineData(ArchiveFormat.SevenZip, ".7z", "7-Zip (.7z)")]
    [InlineData(ArchiveFormat.Zip, ".zip", "ZIP (.zip)")]
    public void FormatHelpers_ReturnExpectedValues(ArchiveFormat format, string extension, string displayName)
    {
        Assert.Equal(extension, format.ToExtension());
        Assert.Equal(displayName, format.ToDisplayName());
    }
}
