using System.ComponentModel.DataAnnotations;

namespace AISmokingDetection.DTOs.ProcessingJob
{
    public class CreateProcessingJobDto
    {
        // Identifier of the existing source video.
        [Required, StringLength(100)]
        public string VideoId { get; set; } = string.Empty;

        public int? CameraId { get; set; }

        // Original capture time (UTC); distinct from processing timestamps.
        public DateTimeOffset? CapturedAt { get; set; }

        // Authorized internal media reference the AI worker uses to fetch the video.
        [StringLength(500)]
        public string? VideoReference { get; set; }

        [StringLength(50)]
        public string? DataOrigin { get; set; }
    }
}
