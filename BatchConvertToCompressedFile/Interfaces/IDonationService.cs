using BatchConvertToCompressedFile.Models;

namespace BatchConvertToCompressedFile.Interfaces;

/// <summary>Verifies donations through the Pure Logic Code donation API.</summary>
internal interface IDonationService
{
    /// <summary>Checks whether an email has a completed donation for this application.</summary>
    /// <param name="email">Donor email address.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The verification outcome, including a user-facing message on failure.</returns>
    Task<DonationVerificationResult> VerifyDonationAsync(
        string email,
        CancellationToken cancellationToken = default);

    /// <summary>Builds the donation page URL for the current application.</summary>
    /// <param name="email">Optional email hint for the donation form.</param>
    /// <returns>A fully qualified URL.</returns>
    string BuildDonationUrl(string? email);
}

/// <summary>Loads and saves the local donation state.</summary>
internal interface IDonationStateStore
{
    /// <summary>Loads the persisted donation state.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The persisted state, or <see cref="DonationState.NotDonated"/>.</returns>
    Task<DonationState> LoadAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a donation state atomically.</summary>
    /// <param name="state">State to save.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task SaveAsync(DonationState state, CancellationToken cancellationToken = default);
}
