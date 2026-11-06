namespace BatchConvertToCompressedFile.Models;

/// <summary>Persisted record of a successful donation verification.</summary>
/// <param name="Donated">Whether the user has donated.</param>
/// <param name="Email">Optional email address that was verified; stored as null to avoid persisting PII.</param>
/// <param name="VerifiedAtUtc">When verification succeeded.</param>
internal sealed record DonationState(bool Donated, string? Email, DateTimeOffset? VerifiedAtUtc)
{
    /// <summary>Gets the default (not donated) state.</summary>
    public static DonationState NotDonated { get; } = new(false, null, null);
}
