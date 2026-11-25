using AISmokingDetection.DTOs.Evidence;

namespace AISmokingDetection.Business.Interfaces
{
    public record EvidenceStream(Stream Stream, string ContentType);

    // Evidence is created only by the AI ingestion flow (IAiCallbackService).
    public interface IEvidenceService
    {
        Task<List<EvidenceResponseDto>> GetEvidenceByIncidentIdAsync(int incidentId);

        Task<EvidenceResponseDto?> GetEvidenceByIdAsync(Guid id);

        /// <summary>Opens the protected evidence content. Throws KeyNotFoundException if missing/unavailable.</summary>
        Task<EvidenceStream> OpenContentAsync(Guid id, CancellationToken ct = default);
    }

    /// <summary>
    /// Resolves an opaque evidence reference to a readable stream.
    /// Swap this implementation if evidence lives in the AI team's / cloud storage.
    /// </summary>
    public interface IEvidenceStorage
    {
        Task<EvidenceStream?> OpenReadAsync(string reference, CancellationToken ct = default);
    }
}
