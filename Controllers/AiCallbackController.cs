using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.DTOs.AiIntegration;
using AISmokingDetection.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISmokingDetection.Controllers
{
    /// <summary>AI -> Backend B callbacks. Service-authenticated; not for staff/web/mobile clients.</summary>
    [ApiController]
    [Route("api/v1/integrations/ai/jobs")]
    [Authorize(Policy = AuthConstants.AiServicePolicy)]
    public class AiCallbackController : ControllerBase
    {
        private readonly IAiCallbackService _callbacks;

        public AiCallbackController(IAiCallbackService callbacks)
        {
            _callbacks = callbacks;
        }

        // 200 = acknowledged (also for duplicates) | 401/403 | 404 unknown job
        // 409 = stale/invalid transition (do not retry) | 422 = payload failed validation
        [HttpPost("{jobId:guid}/status")]
        [ProducesResponseType(typeof(AiCallbackAckDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Status(
            Guid jobId, [FromBody] AiStatusCallbackDto dto, CancellationToken ct)
        {
            var ack = await _callbacks.HandleStatusAsync(jobId, dto, ct);
            return Ok(ack);
        }

        [HttpPost("{jobId:guid}/complete")]
        [ProducesResponseType(typeof(AiCallbackAckDto), StatusCodes.Status200OK)]
        public async Task<IActionResult> Complete(
            Guid jobId, [FromBody] AiCompleteCallbackDto dto, CancellationToken ct)
        {
            var ack = await _callbacks.HandleCompleteAsync(jobId, dto, ct);
            return Ok(ack);
        }
    }
}
