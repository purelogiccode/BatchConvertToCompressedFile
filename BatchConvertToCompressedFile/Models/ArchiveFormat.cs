namespace BatchConvertToCompressedFile.Models;

/// <summary>Output archive format supported by the compression pipeline.</summary>
public enum ArchiveFormat
{
    /// <summary>7-Zip archive (.7z).</summary>
    SevenZip,

    /// <summary>ZIP archive (.zip).</summary>
    Zip
}

/// <summary>File extension helpers for <see cref="ArchiveFormat"/>.</summary>
internal static class ArchiveFormatExtensions
{
    /// <summary>Gets the canonical file extension for the format.</summary>
    public static string ToExtension(this ArchiveFormat format) =>
        format == ArchiveFormat.SevenZip ? ".7z" : ".zip";

    /// <summary>Gets the SharpCompress archive type name used in log messages.</summary>
    public static string ToDisplayName(this ArchiveFormat format) =>
        format == ArchiveFormat.SevenZip ? "7-Zip (.7z)" : "ZIP (.zip)";
}
