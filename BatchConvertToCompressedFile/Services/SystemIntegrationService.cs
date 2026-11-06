using System.Diagnostics;
using BatchConvertToCompressedFile.Interfaces;

namespace BatchConvertToCompressedFile.Services;

/// <summary>Opens URLs and folders using the platform shell.</summary>
internal sealed class SystemIntegrationService : ISystemIntegrationService
{
    /// <inheritdoc />
    public bool OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not open URL {Url}", url);
            return false;
        }
    }

    /// <inheritdoc />
    public bool OpenFolder(string folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath) || !Directory.Exists(folderPath)) return false;

        try
        {
            using var process = Process.Start(new ProcessStartInfo
            {
                FileName = folderPath,
                UseShellExecute = true
            });
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not open folder {FolderPath}", folderPath);
            return false;
        }
    }
}
