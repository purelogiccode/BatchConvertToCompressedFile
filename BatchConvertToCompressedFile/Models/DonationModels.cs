namespace BatchConvertToCompressedFile.Models;

/// <summary>Request sent to the donation verification API.</summary>
/// <param name="Email">Email address used for the donation.</param>
/// <param name="ApplicationSlug">Application the donation supports.</param>
internal sealed record DonationVerificationRequest(string Email, string ApplicationSlug);

/// <summary>Response returned by the donation verification API.</summary>
/// <param name="Donated">Whether at least one completed donation exists.</param>
/// <param name="ApplicationSlug">Normalized application slug.</param>
/// <param name="TotalAmount">Total donated amount.</param>
/// <param name="LastDonationAtUtc">Timestamp of the most recent donation.</param>
internal sealed record DonationVerificationResponse(
    bool Donated,
    string ApplicationSlug,
    decimal TotalAmount,
    DateTime? LastDonationAtUtc);
