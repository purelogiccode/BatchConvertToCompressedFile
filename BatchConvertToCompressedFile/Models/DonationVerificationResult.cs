namespace BatchConvertToCompressedFile.Models;

/// <summary>Outcome of a donation verification request.</summary>
/// <param name="Succeeded">Whether the API request itself succeeded.</param>
/// <param name="Response">Verification payload when <paramref name="Succeeded"/> is true.</param>
/// <param name="Message">User-facing message describing the outcome.</param>
internal sealed record DonationVerificationResult(
    bool Succeeded,
    DonationVerificationResponse? Response,
    string Message)
{
    /// <summary>Gets whether the verified email has a completed donation.</summary>
    public bool Donated => Succeeded && Response?.Donated == true;
}
