using System.Text.Json.Serialization;
using AISmokingDetection.Common;

namespace AISmokingDetection.Model
{
    [JsonConverter(typeof(StringEnumConverter<EvidenceAvailability>))]
    public enum EvidenceAvailability
    {
        Available,
        Unavailable,
        Pending
    }

    public class Evidence
    {
        public Guid EvidenceId { get; set; } = Guid.NewGuid();
        public int IncidentId { get; set; } // FK to Incident
        public string EvidenceType { get; set; } = "video"; // video | image

        // Opaque, protected reference from the AI service. NEVER expose to clients.
        public string? StorageReference { get; set; }

        public EvidenceAvailability AvailabilityStatus { get; set; } = EvidenceAvailability.Available;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public Incident? Incident { get; set; }
    }
}
