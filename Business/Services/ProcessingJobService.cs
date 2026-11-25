using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.Data;
using AISmokingDetection.DTOs.ProcessingJob;
using AISmokingDetection.Model;
using AISmokingDetection.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AISmokingDetection.Business.Services
{
    public class ProcessingJobService : IProcessingJobService
    {
        private readonly ApplicationDbContext _context;
        private readonly IAiJobSubmitter _submitter;
        private readonly AiServiceOptions _options;
        private readonly ILogger<ProcessingJobService> _logger;

        public ProcessingJobService(
            ApplicationDbContext context,
            IAiJobSubmitter submitter,
            IOptions<AiServiceOptions> options,
            ILogger<ProcessingJobService> logger)
        {
            _context = context;
            _submitter = submitter;
            _options = options.Value;
            _logger = logger;
        }

        public async Task<ProcessingJobResponseDto> CreateJobAsync(
            CreateProcessingJobDto dto, CancellationToken ct = default)
        {
            var job = new ProcessingJob
            {
                JobId = Guid.NewGuid(),
                VideoId = dto.VideoId.Trim(),
                CameraId = dto.CameraId,
                CapturedAtUtc = dto.CapturedAt?.ToUniversalTime(),
                VideoReference = dto.VideoReference,
                SchemaVersion = _options.SchemaVersion,
                DataOrigin = string.IsNullOrWhiteSpace(dto.DataOrigin) ? "recorded_video" : dto.DataOrigin!,
                Status = ProcessingJobStatus.Queued
            };

            // Persist first so a fast AI callback always finds the job.
            _context.ProcessingJobs.Add(job);
            await _context.SaveChangesAsync(ct);

            await TrySubmitAsync(job, ct);

            return await GetJobAsync(job.JobId, ct)
                   ?? throw new InvalidOperationException("Job could not be reloaded.");
        }

        public async Task<ProcessingJobResponseDto?> GetJobAsync(Guid jobId, CancellationToken ct = default)
        {
            return await _context.ProcessingJobs
                .AsNoTracking()
                .Where(j => j.JobId == jobId)
                .Select(j => new ProcessingJobResponseDto
                {
                    JobId = j.JobId,
                    VideoId = j.VideoId,
                    CameraId = j.CameraId,
                    CapturedAtUtc = j.CapturedAtUtc,
                    SchemaVersion = j.SchemaVersion,
                    Status = j.Status,
                    ModelVersion = j.ModelVersion,
                    ConfigVersion = j.ConfigVersion,
                    GalleryVersion = j.GalleryVersion,
                    DataOrigin = j.DataOrigin,
                    CreatedAtUtc = j.CreatedAtUtc,
                    UpdatedAtUtc = j.UpdatedAtUtc,
                    SubmittedAtUtc = j.SubmittedAtUtc,
                    SubmissionAttempts = j.SubmissionAttempts,
                    StartedAtUtc = j.StartedAtUtc,
                    CompletedAtUtc = j.CompletedAtUtc,
                    ErrorCode = j.ErrorCode,
                    ErrorMessage = j.ErrorMessage,
                    StuckFlaggedAtUtc = j.StuckFlaggedAtUtc,
                    IncidentCount = j.Incidents.Count
                })
                .FirstOrDefaultAsync(ct);
        }

        public async Task RetryPendingSubmissionsAsync(CancellationToken ct = default)
        {
            // Simple linear backoff: wait (attempts) minutes between tries.
            var now = DateTimeOffset.UtcNow;

            var candidates = await _context.ProcessingJobs
                .Where(j => j.Status == ProcessingJobStatus.Queued
                            && j.SubmittedAtUtc == null
                            && j.SubmissionAttempts < _options.MaxSubmissionAttempts)
                .ToListAsync(ct);

            foreach (var job in candidates)
            {
                var wait = TimeSpan.FromMinutes(Math.Max(1, job.SubmissionAttempts));
                if (job.UpdatedAtUtc + wait > now)
                    continue;

                await TrySubmitAsync(job, ct);
            }
        }

        public async Task FlagStuckJobsAsync(CancellationToken ct = default)
        {
            var cutoff = DateTimeOffset.UtcNow.AddMinutes(-_options.StuckJobTimeoutMinutes);

            // Only jobs the AI accepted but never finished. Job state is NOT changed.
            var stuck = await _context.ProcessingJobs
                .Where(j => (j.Status == ProcessingJobStatus.Queued || j.Status == ProcessingJobStatus.Processing)
                            && j.SubmittedAtUtc != null
                            && j.StuckFlaggedAtUtc == null
                            && j.UpdatedAtUtc < cutoff)
                .ToListAsync(ct);

            if (stuck.Count == 0)
                return;

            var now = DateTimeOffset.UtcNow;
            foreach (var job in stuck)
            {
                job.StuckFlaggedAtUtc = now;
                _logger.LogWarning(
                    "Job {JobId} (video {VideoId}) has had no final callback since {Updated}; flagged as potentially stuck.",
                    job.JobId, job.VideoId, job.UpdatedAtUtc);
            }

            await _context.SaveChangesAsync(ct);
        }

        private async Task TrySubmitAsync(ProcessingJob job, CancellationToken ct)
        {
            var result = await _submitter.SubmitAsync(job, ct);
            var now = DateTimeOffset.UtcNow;

            job.SubmissionAttempts++;
            job.UpdatedAtUtc = now;

            if (result.Accepted)
            {
                job.SubmittedAtUtc = now;
                job.LastSubmissionError = null;
            }
            else
            {
                // Stays Queued and retryable; the monitor retries up to MaxSubmissionAttempts.
                job.LastSubmissionError = Truncate(result.Error, 500);
                _logger.LogWarning("Job {JobId} submission attempt {Attempt} failed: {Error}",
                    job.JobId, job.SubmissionAttempts, result.Error);
            }

            await _context.SaveChangesAsync(ct);
        }

        private static string? Truncate(string? value, int max) =>
            value is null || value.Length <= max ? value : value[..max];
    }
}
