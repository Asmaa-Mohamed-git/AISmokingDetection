using System.Text.Json.Serialization;
using AISmokingDetection.Common;
using AISmokingDetection.Model;

namespace AISmokingDetection.DTOs.ProcessingJob
{
    public class ProcessingJobResponseDto
    {
        public Guid JobId { get; set; }

        public string VideoId { get; set; } = string.Empty;

        public int? CameraId { get; set; }

        public DateTimeOffset? CapturedAtUtc { get; set; }

        public string SchemaVersion { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter<ProcessingJobStatus>))]
        public ProcessingJobStatus Status { get; set; }

        public string? ModelVersion { get; set; }
        public string? ConfigVersion { get; set; }
        public string? GalleryVersion { get; set; }

        public string DataOrigin { get; set; } = string.Empty;

        public DateTimeOffset CreatedAtUtc { get; set; }
        public DateTimeOffset UpdatedAtUtc { get; set; }
        public DateTimeOffset? SubmittedAtUtc { get; set; }
        public int SubmissionAttempts { get; set; }
        public DateTimeOffset? StartedAtUtc { get; set; }
        public DateTimeOffset? CompletedAtUtc { get; set; }

        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }

        // Set when no final callback arrived within the timeout. The job state is unchanged.
        public DateTimeOffset? StuckFlaggedAtUtc { get; set; }

        public int IncidentCount { get; set; }
    }
}
