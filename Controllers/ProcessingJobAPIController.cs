using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.DTOs.ProcessingJob;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISmokingDetection.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ProcessingJobController : ControllerBase
    {
        private readonly IProcessingJobService _processingJobService;

        public ProcessingJobController(IProcessingJobService processingJobService)
        {
            _processingJobService = processingJobService;
        }

        /// <summary>Creates a job (Queued) and submits it to the AI service. Returns 202.</summary>
        [HttpPost]
        public async Task<IActionResult> CreateJob(
            [FromBody] CreateProcessingJobDto dto, CancellationToken ct)
        {
            var result = await _processingJobService.CreateJobAsync(dto, ct);

            return AcceptedAtAction(
                nameof(GetJob),
                new { jobId = result.JobId },
                result);
        }

        [HttpGet("{jobId:guid}")]
        public async Task<IActionResult> GetJob(Guid jobId, CancellationToken ct)
        {
            var result = await _processingJobService.GetJobAsync(jobId, ct);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        // Status changes (start / complete / fail) are no longer exposed here:
        // they arrive only through the authenticated AI callback endpoints.
    }
}
