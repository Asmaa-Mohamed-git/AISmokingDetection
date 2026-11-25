using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.Common;
using AISmokingDetection.Data;
using AISmokingDetection.DTOs.AiIntegration;
using AISmokingDetection.Model;
using Microsoft.EntityFrameworkCore;

namespace AISmokingDetection.Business.Services
{
    /// <summary>
    /// Handles AI -> Backend B callbacks (contract sections 5.2, 5.3, 7, 8).
    /// Authenticate -> validate -> idempotency check -> persist in one transaction -> ack.
    /// </summary>
    public class AiCallbackService : IAiCallbackService
    {
        private const int MaxEvidenceReferenceLength = 500;
        private const int MaxErrorCodeLength = 100;
        private const int MaxErrorMessageLength = 500;
        private const int MaxTrackIdLength = 100;
        private const int MaxVersionLength = 100;

        private static readonly HashSet<string> AllowedEvidenceTypes =
            new(StringComparer.OrdinalIgnoreCase) { "video", "image" };

        private readonly ApplicationDbContext _context;
        private readonly ILogger<AiCallbackService> _logger;

        public AiCallbackService(ApplicationDbContext context, ILogger<AiCallbackService> logger)
        {
            _context = context;
            _logger = logger;
        }

        // ------------------------------------------------------------------
        // Status callback: Processing | Failed
        // ------------------------------------------------------------------
        public async Task<AiCallbackAckDto> HandleStatusAsync(
            Guid routeJobId, AiStatusCallbackDto dto, CancellationToken ct = default)
        {
            if (dto.JobId != routeJobId)
                throw new ValidationFailedException("jobId in the body does not match the route.");

            if (!Enum.IsDefined(dto.Status))
                throw new ValidationFailedException("status must be Processing or Failed.");

            var job = await GetJobAsync(routeJobId, ct);
            var now = DateTimeOffset.UtcNow;
            var at = dto.UpdatedAt == default ? now : dto.UpdatedAt.ToUniversalTime();

            if (dto.Status == AiCallbackStatus.Processing)
            {
                switch (job.Status)
                {
                    case ProcessingJobStatus.Queued:
                        job.Status = ProcessingJobStatus.Processing;
                        job.StartedAtUtc = at;
                        job.LastStatusUpdatedAtUtc = at;
                        job.UpdatedAtUtc = now;
                        await _context.SaveChangesAsync(ct);
                        return Ack(job, "accepted");

                    case ProcessingJobStatus.Processing:
                        return Ack(job, "duplicate"); // idempotent re-delivery

                    default:
                        // Completed / Failed: a late Processing callback is stale.
                        _logger.LogWarning("Stale Processing callback ignored for job {JobId} (status {Status}).",
                            job.JobId, job.Status);
                        throw new InvalidOperationException(
                            $"Stale transition: job is already {job.Status}.");
                }
            }

            // ---- Failed ----
            if (string.IsNullOrWhiteSpace(dto.ErrorCode))
                throw new ValidationFailedException("errorCode is required when status is Failed.");

            switch (job.Status)
            {
                case ProcessingJobStatus.Failed:
                    return Ack(job, "duplicate");

                case ProcessingJobStatus.Completed:
                    _logger.LogWarning("Stale Failed callback ignored for completed job {JobId}.", job.JobId);
                    throw new InvalidOperationException("Stale transition: job is already Completed.");

                default: // Queued or Processing
                    job.Status = ProcessingJobStatus.Failed;
                    job.ErrorCode = Truncate(dto.ErrorCode.Trim(), MaxErrorCodeLength);
                    job.ErrorMessage = Truncate(dto.ErrorMessage, MaxErrorMessageLength);
                    job.CompletedAtUtc = at;
                    job.LastStatusUpdatedAtUtc = at;
                    job.UpdatedAtUtc = now;
                    await _context.SaveChangesAsync(ct);
                    return Ack(job, "accepted");
            }
        }

        // ------------------------------------------------------------------
        // Final result callback
        // ------------------------------------------------------------------
        public async Task<AiCallbackAckDto> HandleCompleteAsync(
            Guid routeJobId, AiCompleteCallbackDto dto, CancellationToken ct = default)
        {
            if (dto.JobId != routeJobId)
                throw new ValidationFailedException("jobId in the body does not match the route.");

            var job = await GetJobAsync(routeJobId, ct);

            // Idempotency / transition checks before heavy validation.
            if (job.Status == ProcessingJobStatus.Completed)
                return Ack(job, "duplicate");

            if (job.Status == ProcessingJobStatus.Failed)
            {
                _logger.LogWarning("Completion callback ignored for failed job {JobId}.", job.JobId);
                throw new InvalidOperationException("Stale transition: job is already Failed.");
            }

            ValidateComplete(dto, job);

            var events = dto.Events!; // validated non-null
            var eventIds = events.Select(e => e.EventId).ToList();

            // Event IDs that already exist (retried delivery) are skipped; IDs owned by another job are a conflict.
            var existing = await _context.Incidents
                .Where(i => eventIds.Contains(i.EventId))
                .Select(i => new { i.EventId, i.ProcessingJobId })
                .ToListAsync(ct);

            if (existing.Any(x => x.ProcessingJobId != job.ProcessingJobId))
                throw new InvalidOperationException("One or more eventIds already belong to a different job.");

            var alreadyStored = existing.Select(x => x.EventId).ToHashSet();
            var now = DateTimeOffset.UtcNow;
            var created = 0;

            await using var tx = await _context.Database.BeginTransactionAsync(ct);
            try
            {
                foreach (var e in events.Where(e => !alreadyStored.Contains(e.EventId)))
                {
                    var incident = new Incident
                    {
                        EventId = e.EventId,
                        ProcessingJobId = job.ProcessingJobId,
                        TrackId = e.TrackId.Trim(),
                        CameraId = job.CameraId,
                        EventType = e.EventType,
                        StartTimeSeconds = e.StartTimeSeconds,
                        EndTimeSeconds = e.EndTimeSeconds,
                        ConfidenceScore = Math.Round((decimal)e.EventScore, 4),
                        IdentityStatus = e.IdentityStatus,
                        ParticipantId = e.IdentityStatus == IdentityStatus.MatchedCandidate
                            ? e.ParticipantId
                            : null,
                        // AI output is a candidate; staff review decides.
                        Status = IncidentStatus.Suspected,
                        ReviewStatus = ReviewStatus.Pending,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (!string.IsNullOrWhiteSpace(e.EvidenceReference))
                    {
                        incident.Evidences.Add(new Evidence
                        {
                            EvidenceId = Guid.NewGuid(),
                            EvidenceType = string.IsNullOrWhiteSpace(e.EvidenceType)
                                ? "video"
                                : e.EvidenceType.Trim().ToLowerInvariant(),
                            StorageReference = e.EvidenceReference.Trim(),
                            AvailabilityStatus = EvidenceAvailability.Available,
                            CreatedAt = DateTime.UtcNow
                        });
                    }

                    _context.Incidents.Add(incident);
                    created++;
                }

                var completedAt = dto.CompletedAt?.ToUniversalTime() ?? now;

                job.Status = ProcessingJobStatus.Completed;
                job.ModelVersion = Truncate(dto.ModelVersion, MaxVersionLength);
                job.ConfigVersion = Truncate(dto.ConfigVersion, MaxVersionLength);
                job.GalleryVersion = Truncate(dto.GalleryVersion, MaxVersionLength);
                job.CompletedAtUtc = completedAt;
                job.LastStatusUpdatedAtUtc = completedAt;
                job.UpdatedAtUtc = now;

                // Job is marked Completed only if everything above is persisted.
                await _context.SaveChangesAsync(ct);
                await tx.CommitAsync(ct);
            }
            catch (DbUpdateException ex)
            {
                await tx.RollbackAsync(ct);
                _context.ChangeTracker.Clear();

                // Concurrent duplicate delivery may have won the race on the unique EventId index.
                var current = await _context.ProcessingJobs.AsNoTracking()
                    .FirstOrDefaultAsync(j => j.JobId == routeJobId, ct);

                if (current?.Status == ProcessingJobStatus.Completed)
                    return Ack(current, "duplicate");

                _logger.LogError(ex, "Failed to persist result for job {JobId}.", routeJobId);
                throw; // 500 -> AI retries with the same jobId/eventIds
            }

            return Ack(job, "accepted", created);
        }

        // ------------------------------------------------------------------
        // Validation (contract section 7)
        // ------------------------------------------------------------------
        private static void ValidateComplete(AiCompleteCallbackDto dto, ProcessingJob job)
        {
            var errors = new List<string>();

            if (!string.Equals(dto.VideoId, job.VideoId, StringComparison.Ordinal))
                errors.Add("videoId does not match the video this job was created for.");

            if (!string.Equals(dto.Status, "Completed", StringComparison.OrdinalIgnoreCase))
                errors.Add("status must be 'Completed' on the completion endpoint.");

            if (dto.Events is null)
            {
                errors.Add("events is required (use an empty array for a zero-event result).");
                throw new ValidationFailedException(errors);
            }

            var seen = new HashSet<Guid>();

            for (int i = 0; i < dto.Events.Count; i++)
            {
                var e = dto.Events[i];
                var p = $"events[{i}]";

                if (e is null) { errors.Add($"{p} is null."); continue; }

                if (e.EventId == Guid.Empty)
                    errors.Add($"{p}.eventId is required.");
                else if (!seen.Add(e.EventId))
                    errors.Add($"{p}.eventId is duplicated within the payload.");

                if (string.IsNullOrWhiteSpace(e.TrackId))
                    errors.Add($"{p}.trackId is required.");
                else if (e.TrackId.Length > MaxTrackIdLength)
                    errors.Add($"{p}.trackId is too long.");

                if (!Enum.IsDefined(e.EventType))
                    errors.Add($"{p}.eventType is not an allowed value.");

                if (!Enum.IsDefined(e.IdentityStatus))
                    errors.Add($"{p}.identityStatus is not an allowed value.");

                if (!double.IsFinite(e.EventScore) || e.EventScore < 0.0 || e.EventScore > 1.0)
                    errors.Add($"{p}.eventScore must be between 0.0 and 1.0.");

                if (!double.IsFinite(e.StartTimeSeconds) || e.StartTimeSeconds < 0)
                    errors.Add($"{p}.startTimeSeconds must be non-negative.");

                if (!double.IsFinite(e.EndTimeSeconds) || e.EndTimeSeconds <= e.StartTimeSeconds)
                    errors.Add($"{p}.endTimeSeconds must be greater than startTimeSeconds.");

                // Identity rules
                if (e.IdentityStatus != IdentityStatus.MatchedCandidate && e.ParticipantId is not null)
                    errors.Add($"{p}.participantId must be null unless identityStatus is matched_candidate.");

                if (e.ParticipantId is not null && e.ParticipantId <= 0)
                    errors.Add($"{p}.participantId must be a positive id.");

                // Evidence
                if (e.EvidenceReference is not null && e.EvidenceReference.Length > MaxEvidenceReferenceLength)
                    errors.Add($"{p}.evidenceReference is too long.");

                if (!string.IsNullOrWhiteSpace(e.EvidenceType) && !AllowedEvidenceTypes.Contains(e.EvidenceType.Trim()))
                    errors.Add($"{p}.evidenceType must be 'video' or 'image'.");
            }

            if (errors.Count > 0)
                throw new ValidationFailedException(errors);
        }

        // ------------------------------------------------------------------
        private async Task<ProcessingJob> GetJobAsync(Guid jobId, CancellationToken ct)
        {
            return await _context.ProcessingJobs.FirstOrDefaultAsync(j => j.JobId == jobId, ct)
                   ?? throw new KeyNotFoundException("Processing job not found.");
        }

        private static AiCallbackAckDto Ack(ProcessingJob job, string outcome, int created = 0) => new()
        {
            JobId = job.JobId,
            Status = job.Status,
            Outcome = outcome,
            IncidentsCreated = created
        };

        private static string? Truncate(string? value, int max) =>
            value is null || value.Length <= max ? value : value[..max];
    }
}
