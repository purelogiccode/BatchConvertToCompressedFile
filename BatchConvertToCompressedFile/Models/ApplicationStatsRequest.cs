namespace BatchConvertToCompressedFile.Models;

/// <summary>Payload sent to the application statistics endpoint at startup.</summary>
/// <param name="ApplicationId">Stable application identifier.</param>
/// <param name="Version">Running application version.</param>
internal sealed record ApplicationStatsRequest(string ApplicationId, string Version);
