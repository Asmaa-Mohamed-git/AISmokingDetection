using System.Text.Json.Serialization;
using AISmokingDetection.Common;

namespace AISmokingDetection.Model
{
    [JsonConverter(typeof(StringEnumConverter<IncidentStatus>))]
    public enum IncidentStatus
    {
        Suspected,
        Unresolved,
        Confirmed,
        Rejected,
        Resolved
    }

    // Wire format (AI contract): smoking | vaping | unknown
    [JsonConverter(typeof(SnakeCaseLowerEnumConverter<EventType>))]
    public enum EventType
    {
        Smoking,
        Vaping,
        Unknown
    }

    // Wire format (AI contract): matched_candidate | unknown | insufficient_quality
    [JsonConverter(typeof(SnakeCaseLowerEnumConverter<IdentityStatus>))]
    public enum IdentityStatus
    {
        MatchedCandidate,
        Unknown,
        InsufficientQuality
    }

    [JsonConverter(typeof(StringEnumConverter<ReviewStatus>))]
    public enum ReviewStatus
    {
        Pending,
        InReview,
        Reviewed
    }

    public class Incident
    {
        public int IncidentId { get; set; }

        // Stable across retries; unique (idempotency key).
        public Guid EventId { get; set; }

        public int ProcessingJobId { get; set; } // FK to ProcessingJob (internal key)

        public string TrackId { get; set; } = string.Empty;

        public int? CameraId { get; set; }

        // Candidate only; null unless IdentityStatus == MatchedCandidate.
        public int? ParticipantId { get; set; }

        public EventType EventType { get; set; } = EventType.Unknown;

        // Offsets relative to the beginning of the source video.
        public double StartTimeSeconds { get; set; }
        public double EndTimeSeconds { get; set; }

        public IncidentStatus Status { get; set; } = IncidentStatus.Suspected;
        public IdentityStatus IdentityStatus { get; set; } = IdentityStatus.Unknown;
        public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Pending;

        public decimal? ConfidenceScore { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ProcessingJob? ProcessingJob { get; set; }
        public ICollection<Evidence> Evidences { get; set; } = new List<Evidence>();
        public ICollection<ReviewHistory> ReviewHistories { get; set; } = new List<ReviewHistory>();
    }
}
