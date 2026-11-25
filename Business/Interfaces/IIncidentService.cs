using AISmokingDetection.DTOs.Incident;

namespace AISmokingDetection.Business.Interfaces
{
    // Incidents are created only by the AI ingestion flow (IAiCallbackService).
    public interface IIncidentService
    {
        Task<List<IncidentResponseDto>> GetAllIncidentsAsync();

        Task<IncidentResponseDto?> GetIncidentByIdAsync(int id);
    }
}
