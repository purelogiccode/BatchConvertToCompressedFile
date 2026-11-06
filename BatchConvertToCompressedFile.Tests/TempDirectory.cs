namespace BatchConvertToCompressedFile.Tests;

/// <summary>Creates a unique temporary directory and removes it on dispose.</summary>
internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(
            System.IO.Path.GetTempPath(),
            "BatchConvertToCompressedFile.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string CreateFile(string relativePath, int byteCount = 4096)
    {
        var fullPath = System.IO.Path.Combine(Path, relativePath);
        var directory = System.IO.Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        var bytes = new byte[byteCount];
        Random.Shared.NextBytes(bytes);

        // Make the payload compressible so archive tests stay fast.
        for (var index = 0; index < bytes.Length; index++)
        {
            bytes[index] = (byte)(index % 251);
        }

        File.WriteAllBytes(fullPath, bytes);
        return fullPath;
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(Path)) Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup.
        }
        catch (UnauthorizedAccessException)
        {
            // Best-effort cleanup.
        }
    }
}
