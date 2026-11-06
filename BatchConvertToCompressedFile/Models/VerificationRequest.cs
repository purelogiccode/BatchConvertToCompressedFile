namespace BatchConvertToCompressedFile.Models;

/// <summary>Describes a batch archive verification request.</summary>
/// <param name="InputFolder">Folder containing the archives to verify.</param>
/// <param name="IncludeSubfolders">Whether subfolders are scanned recursively.</param>
/// <param name="MoveSuccessFiles">Whether successfully verified archives are moved.</param>
/// <param name="SuccessFolder">Destination folder for successful archives.</param>
/// <param name="MoveFailedFiles">Whether failed archives are moved.</param>
/// <param name="FailedFolder">Destination folder for failed archives.</param>
internal sealed record VerificationRequest(
    string InputFolder,
    bool IncludeSubfolders,
    bool MoveSuccessFiles,
    string? SuccessFolder,
    bool MoveFailedFiles,
    string? FailedFolder);
