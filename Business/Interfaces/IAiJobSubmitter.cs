using AISmokingDetection.Model;

namespace AISmokingDetection.Business.Interfaces
{
    public record AiSubmitResult(bool Accepted, string? Error);

    /// <summary>Backend B -> AI: submit a job (contract 5.1).</summary>
    public interface IAiJobSubmitter
    {
        Task<AiSubmitResult> SubmitAsync(ProcessingJob job, CancellationToken ct = default);
    }
}
