using AISmokingDetection.Business.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISmokingDetection.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class IncidentController : ControllerBase
    {
        private readonly IIncidentService _incidentService;

        public IncidentController(IIncidentService incidentService)
        {
            _incidentService = incidentService;
        }

        // Incidents are created only by the AI ingestion flow, so there is no public POST.

        [HttpGet]
        public async Task<IActionResult> GetAllIncidents()
        {
            var result = await _incidentService.GetAllIncidentsAsync();
            return Ok(result);
        }

        [HttpGet("{id:int}")]
        [ProducesResponseType(statusCode: 200)]
        [ProducesResponseType(statusCode: 400)]
        [ProducesResponseType(statusCode: 404)]
        public async Task<IActionResult> GetIncidentById(int id)
        {
            if (id <= 0)
                return BadRequest("Invalid incident ID.");

            var result = await _incidentService.GetIncidentByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}
