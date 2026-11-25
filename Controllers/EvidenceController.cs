using AISmokingDetection.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISmokingDetection.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class EvidenceController : ControllerBase
    {
        private readonly IEvidenceService _evidenceService;

        public EvidenceController(IEvidenceService evidenceService)
        {
            _evidenceService = evidenceService;
        }

        // Evidence is created only by the AI ingestion flow, so there is no public POST.

        [HttpGet("{id:guid}")]
        public async Task<IActionResult> GetEvidenceById(Guid id)
        {
            var result = await _evidenceService.GetEvidenceByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [HttpGet("incident/{incidentId:int}")]
        public async Task<IActionResult> GetEvidenceByIncidentId(int incidentId)
        {
            var result = await _evidenceService.GetEvidenceByIncidentIdAsync(incidentId);
            return Ok(result);
        }

        /// <summary>Authorized access to the evidence media. The storage reference is never exposed.</summary>
        [HttpGet("{id:guid}/content")]
        public async Task<IActionResult> GetEvidenceContent(Guid id, CancellationToken ct)
        {
            var content = await _evidenceService.OpenContentAsync(id, ct);

            // Range processing lets browsers/mobile players seek within video clips.
            return File(content.Stream, content.ContentType, enableRangeProcessing: true);
        }
    }
}
