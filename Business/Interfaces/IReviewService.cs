using AISmokingDetection.DTOs.ReviewHistory;

namespace AISmokingDetection.Business.Interfaces
{
    public interface IReviewService
    {
        Task<ReviewHistoryResponseDto> CreateReviewAsync(
            int incidentId,
            int reviewerId,
            CreateReviewDto dto);

        Task<List<ReviewHistoryResponseDto>> GetReviewsByIncidentIdAsync(
            int incidentId);
    }
}
