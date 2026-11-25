using System.Text.Json.Serialization;
using AISmokingDetection.Common;
using AISmokingDetection.Model;

namespace AISmokingDetection.DTOs.Evidence
{
    // Intentionally has NO storage reference: evidence is retrieved through
    // GET /api/Evidence/{id}/content (authorized), never via a raw path or URL.
    public class EvidenceResponseDto
    {
        public Guid EvidenceId { get; set; }

        public int IncidentId { get; set; }

        public string EvidenceType { get; set; } = string.Empty;

        [JsonConverter(typeof(StringEnumConverter<EvidenceAvailability>))]
        public EvidenceAvailability AvailabilityStatus { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
