using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.Data;
using AISmokingDetection.DTOs.Evidence;
using AISmokingDetection.Model;
using Microsoft.EntityFrameworkCore;

namespace AISmokingDetection.Business.Services
{
    public class EvidenceService : IEvidenceService
    {
        private readonly ApplicationDbContext _context;
        private readonly IEvidenceStorage _storage;
        private readonly ILogger<EvidenceService> _logger;

        public EvidenceService(
            ApplicationDbContext context,
            IEvidenceStorage storage,
            ILogger<EvidenceService> logger)
        {
            _context = context;
            _storage = storage;
            _logger = logger;
        }

        public async Task<List<EvidenceResponseDto>> GetEvidenceByIncidentIdAsync(int incidentId)
        {
            var evidences = await _context.Evidences
                .AsNoTracking()
                .Where(e => e.IncidentId == incidentId)
                .ToListAsync();

            return evidences.Select(MapToResponseDto).ToList();
        }

        public async Task<EvidenceResponseDto?> GetEvidenceByIdAsync(Guid id)
        {
            var evidence = await _context.Evidences
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.EvidenceId == id);

            return evidence == null ? null : MapToResponseDto(evidence);
        }

        public async Task<EvidenceStream> OpenContentAsync(Guid id, CancellationToken ct = default)
        {
            var evidence = await _context.Evidences.FirstOrDefaultAsync(e => e.EvidenceId == id, ct)
                           ?? throw new KeyNotFoundException("Evidence not found.");

            if (string.IsNullOrWhiteSpace(evidence.StorageReference))
                throw new KeyNotFoundException("Evidence content is not available.");

            var content = await _storage.OpenReadAsync(evidence.StorageReference, ct);

            if (content == null)
            {
                // Keep the incident distinguishable from accessible evidence.
                if (evidence.AvailabilityStatus != EvidenceAvailability.Unavailable)
                {
                    evidence.AvailabilityStatus = EvidenceAvailability.Unavailable;
                    await _context.SaveChangesAsync(ct);
                    _logger.LogWarning("Evidence {EvidenceId} content could not be opened; marked unavailable.", id);
                }

                throw new KeyNotFoundException("Evidence content is not available.");
            }

            if (evidence.AvailabilityStatus != EvidenceAvailability.Available)
            {
                evidence.AvailabilityStatus = EvidenceAvailability.Available;
                await _context.SaveChangesAsync(ct);
            }

            return content;
        }

        private static EvidenceResponseDto MapToResponseDto(Evidence evidence)
        {
            return new EvidenceResponseDto
            {
                EvidenceId = evidence.EvidenceId,
                IncidentId = evidence.IncidentId,
                EvidenceType = evidence.EvidenceType,
                AvailabilityStatus = evidence.AvailabilityStatus,
                CreatedAt = evidence.CreatedAt
            };
        }
    }
}
