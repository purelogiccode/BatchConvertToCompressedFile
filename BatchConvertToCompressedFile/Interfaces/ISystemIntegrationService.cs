namespace BatchConvertToCompressedFile.Interfaces;

/// <summary>Opens external URLs and folders with the operating system shell.</summary>
internal interface ISystemIntegrationService
{
    /// <summary>Opens a URL in the user's default browser.</summary>
    /// <param name="url">Absolute URL.</param>
    /// <returns><see langword="true"/> when the shell accepted the request.</returns>
    bool OpenUrl(string url);

    /// <summary>Reveals a folder in the operating system file manager.</summary>
    /// <param name="folderPath">Absolute folder path.</param>
    /// <returns><see langword="true"/> when the shell accepted the request.</returns>
    bool OpenFolder(string folderPath);
}
