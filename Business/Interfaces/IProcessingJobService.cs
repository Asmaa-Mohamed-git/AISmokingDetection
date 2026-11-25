using AISmokingDetection.DTOs.ProcessingJob;

namespace AISmokingDetection.Business.Interfaces
{
    public interface IProcessingJobService
    {
        /// <summary>Creates a Queued job and tries to submit it to the AI service.</summary>
        Task<ProcessingJobResponseDto> CreateJobAsync(CreateProcessingJobDto dto, CancellationToken ct = default);

        Task<ProcessingJobResponseDto?> GetJobAsync(Guid jobId, CancellationToken ct = default);

        /// <summary>Retries submission for Queued jobs the AI service never accepted.</summary>
        Task RetryPendingSubmissionsAsync(CancellationToken ct = default);

        /// <summary>Flags non-terminal submitted jobs that exceeded the timeout. Never assumes success.</summary>
        Task FlagStuckJobsAsync(CancellationToken ct = default);
    }
}
