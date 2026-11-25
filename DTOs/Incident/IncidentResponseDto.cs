using AISmokingDetection.DTOs.Evidence;
using AISmokingDetection.Model;

namespace AISmokingDetection.DTOs.Incident
{
    public class IncidentResponseDto
    {
        public int IncidentId { get; set; }

        public Guid EventId { get; set; }

        // Public job identifier and source video.
        public Guid JobId { get; set; }

        public string VideoId { get; set; } = string.Empty;

        public string TrackId { get; set; } = string.Empty;

        public int? CameraId { get; set; }

        // Candidate only; null unless identityStatus is matched_candidate.
        public int? ParticipantId { get; set; }

        public EventType EventType { get; set; }

        public double StartTimeSeconds { get; set; }

        public double EndTimeSeconds { get; set; }

        public IncidentStatus Status { get; set; }

        public IdentityStatus IdentityStatus { get; set; }

        public ReviewStatus ReviewStatus { get; set; }

        public decimal? ConfidenceScore { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public List<EvidenceResponseDto> Evidences { get; set; } = new();
    }
}
