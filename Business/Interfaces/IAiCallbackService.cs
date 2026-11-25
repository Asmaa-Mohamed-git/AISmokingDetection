using AISmokingDetection.DTOs.AiIntegration;

namespace AISmokingDetection.Business.Interfaces
{
    public interface IAiCallbackService
    {
        Task<AiCallbackAckDto> HandleStatusAsync(Guid routeJobId, AiStatusCallbackDto dto, CancellationToken ct = default);

        Task<AiCallbackAckDto> HandleCompleteAsync(Guid routeJobId, AiCompleteCallbackDto dto, CancellationToken ct = default);
    }
}
