using BatchConvertToCompressedFile.Models;

namespace BatchConvertToCompressedFile.Interfaces;

/// <summary>Creates and verifies archive files.</summary>
internal interface IArchiveService
{
    /// <summary>Compresses a single file into a new archive.</summary>
    /// <param name="inputFile">Absolute path of the source file.</param>
    /// <param name="outputFile">Absolute path of the archive to create.</param>
    /// <param name="format">Archive format.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see cref="ProcessStatus.Success"/> when the archive is complete.</returns>
    Task<ProcessStatus> CompressFileAsync(
        string inputFile,
        string outputFile,
        ArchiveFormat format,
        CancellationToken cancellationToken);

    /// <summary>Verifies the integrity of an archive by reading every entry.</summary>
    /// <param name="archivePath">Absolute path of the archive.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when the archive is valid.</returns>
    Task<bool> VerifyArchiveAsync(string archivePath, CancellationToken cancellationToken);
}
