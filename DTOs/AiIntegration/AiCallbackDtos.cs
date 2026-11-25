using System.Text.Json.Serialization;
using AISmokingDetection.Common;
using AISmokingDetection.Model;

namespace AISmokingDetection.DTOs.AiIntegration
{
    /// <summary>Statuses the AI service may send to the status endpoint (contract 5.2).</summary>
    [JsonConverter(typeof(StringEnumConverter<AiCallbackStatus>))]
    public enum AiCallbackStatus
    {
        Processing,
        Failed
    }

    /// <summary>POST /api/v1/integrations/ai/jobs/{jobId}/status</summary>
    public class AiStatusCallbackDto
    {
        public Guid JobId { get; set; }

        public required AiCallbackStatus Status { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        // Required when Status == Failed.
        public string? ErrorCode { get; set; }

        // Safe diagnostic message (no stack traces / internal paths).
        public string? ErrorMessage { get; set; }
    }

    /// <summary>POST /api/v1/integrations/ai/jobs/{jobId}/complete</summary>
    public class AiCompleteCallbackDto
    {
        public Guid JobId { get; set; }

        public string VideoId { get; set; } = string.Empty;

        // Must be "Completed".
        public string Status { get; set; } = string.Empty;

        public string? ModelVersion { get; set; }
        public string? ConfigVersion { get; set; }
        public string? GalleryVersion { get; set; }
        public string? SchemaVersion { get; set; }

        public DateTimeOffset? CompletedAt { get; set; }

        // Required. An empty array is a valid, successful zero-event result.
        public List<AiEventDto>? Events { get; set; }
    }

    public class AiEventDto
    {
        public required Guid EventId { get; set; }

        public string TrackId { get; set; } = string.Empty;

        public required EventType EventType { get; set; }

        public required double EventScore { get; set; }

        public required double StartTimeSeconds { get; set; }

        public required double EndTimeSeconds { get; set; }

        public required IdentityStatus IdentityStatus { get; set; }

        public int? ParticipantId { get; set; }

        // Opaque reference to the evidence clip. Never exposed to clients.
        public string? EvidenceReference { get; set; }

        // "video" (default) or "image".
        public string? EvidenceType { get; set; }
    }

    /// <summary>Acknowledgement returned to the AI service.</summary>
    public class AiCallbackAckDto
    {
        public Guid JobId { get; set; }

        [JsonConverter(typeof(StringEnumConverter<ProcessingJobStatus>))]
        public ProcessingJobStatus Status { get; set; }

        // "accepted" or "duplicate"
        public string Outcome { get; set; } = "accepted";

        public int IncidentsCreated { get; set; }
    }
}
