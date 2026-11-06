namespace BatchConvertToCompressedFile.Models;

/// <summary>Describes a batch compression request.</summary>
/// <param name="InputFolder">Folder containing the files to compress.</param>
/// <param name="OutputFolder">Folder that receives the generated archives.</param>
/// <param name="Format">Output archive format.</param>
/// <param name="DeleteOriginals">Whether source files are deleted after successful compression.</param>
internal sealed record CompressionRequest(
    string InputFolder,
    string OutputFolder,
    ArchiveFormat Format,
    bool DeleteOriginals);
