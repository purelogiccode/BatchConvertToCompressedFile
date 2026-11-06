using System.Text.Json;
using BatchConvertToCompressedFile.Interfaces;
using BatchConvertToCompressedFile.Models;

namespace BatchConvertToCompressedFile.Services;

/// <summary>Persists the donation state in the per-user application data folder.</summary>
internal sealed class DonationStateStore : IDonationStateStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true
    };

    private readonly string _filePath;

    /// <summary>Initializes a new state store.</summary>
    /// <param name="filePath">Optional state-file path override for tests.</param>
    public DonationStateStore(string? filePath = null)
    {
        _filePath = filePath ?? PathUtilities.GetDonationStateFilePath();
    }

    /// <inheritdoc />
    public async Task<DonationState> LoadAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_filePath)) return DonationState.NotDonated;

            await using var stream = File.OpenRead(_filePath);
            var state = await JsonSerializer.DeserializeAsync<DonationState>(
                stream, SerializerOptions, cancellationToken);

            return state ?? DonationState.NotDonated;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException)
        {
            // A missing/corrupt state file is an expected local condition; never a bug report.
            Log.Information("Could not load donation state from {FilePath}: {Message}", _filePath, ex.Message);
            return DonationState.NotDonated;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while loading donation state from {FilePath}", _filePath);
            return DonationState.NotDonated;
        }
    }

    /// <inheritdoc />
    public async Task SaveAsync(DonationState state, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(state);

        try
        {
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

            var temporaryPath = $"{_filePath}.{Guid.NewGuid():N}.tmp";
            try
            {
                await using (var stream = new FileStream(
                                 temporaryPath, FileMode.Create, FileAccess.Write, FileShare.None,
                                 4096, FileOptions.Asynchronous))
                {
                    await JsonSerializer.SerializeAsync(
                        stream, state, SerializerOptions, cancellationToken);
                }

                File.Move(temporaryPath, _filePath, overwrite: true);
            }
            finally
            {
                if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
            }
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not save donation state to {FilePath}", _filePath);
        }
    }
}
