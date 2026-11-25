using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.Data;
using AISmokingDetection.DTOs.Evidence;
using AISmokingDetection.DTOs.Incident;
using AISmokingDetection.Model;
using Microsoft.EntityFrameworkCore;

namespace AISmokingDetection.Business.Services
{
    public class IncidentService : IIncidentService
    {
        private readonly ApplicationDbContext _context;

        public IncidentService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<IncidentResponseDto>> GetAllIncidentsAsync()
        {
            var incidents = await _context.Incidents
                .AsNoTracking()
                .Include(i => i.Evidences)
                .Include(i => i.ProcessingJob)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            return incidents.Select(MapToResponseDto).ToList();
        }

        public async Task<IncidentResponseDto?> GetIncidentByIdAsync(int id)
        {
            var incident = await _context.Incidents
                .AsNoTracking()
                .Include(i => i.Evidences)
                .Include(i => i.ProcessingJob)
                .FirstOrDefaultAsync(i => i.IncidentId == id);

            return incident == null ? null : MapToResponseDto(incident);
        }

        private static IncidentResponseDto MapToResponseDto(Incident incident)
        {
            return new IncidentResponseDto
            {
                IncidentId = incident.IncidentId,
                EventId = incident.EventId,
                JobId = incident.ProcessingJob?.JobId ?? Guid.Empty,
                VideoId = incident.ProcessingJob?.VideoId ?? string.Empty,
                TrackId = incident.TrackId,
                CameraId = incident.CameraId,
                ParticipantId = incident.ParticipantId,
                EventType = incident.EventType,
                StartTimeSeconds = incident.StartTimeSeconds,
                EndTimeSeconds = incident.EndTimeSeconds,
                Status = incident.Status,
                IdentityStatus = incident.IdentityStatus,
                ReviewStatus = incident.ReviewStatus,
                ConfidenceScore = incident.ConfidenceScore,
                CreatedAt = incident.CreatedAt,
                UpdatedAt = incident.UpdatedAt,

                // No StorageReference: clients fetch content via the authorized evidence endpoint.
                Evidences = incident.Evidences.Select(e => new EvidenceResponseDto
                {
                    EvidenceId = e.EvidenceId,
                    IncidentId = e.IncidentId,
                    EvidenceType = e.EvidenceType,
                    AvailabilityStatus = e.AvailabilityStatus,
                    CreatedAt = e.CreatedAt
                }).ToList()
            };
        }
    }
}
