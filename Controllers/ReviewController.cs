using System.Security.Claims;
using AISmokingDetection.Business.Interfaces;
using AISmokingDetection.DTOs.ReviewHistory;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AISmokingDetection.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReviewController : ControllerBase
    {
        private readonly IReviewService _reviewService;

        public ReviewController(IReviewService reviewService)
        {
            _reviewService = reviewService;
        }

        [HttpPost("incident/{incidentId:int}")]
        public async Task<IActionResult> CreateReview(
            int incidentId,
            [FromBody] CreateReviewDto dto)
        {
            // Reviewer identity comes from the token so the audit trail cannot be spoofed.
            var idClaim = User.FindFirstValue(ClaimTypes.NameIdentifier)
                          ?? User.FindFirstValue("sub");

            if (!int.TryParse(idClaim, out var reviewerId))
                return Forbid();

            var result = await _reviewService.CreateReviewAsync(incidentId, reviewerId, dto);

            return CreatedAtAction(
                nameof(GetReviewsByIncidentId),
                new { incidentId },
                result);
        }

        [HttpGet("incident/{incidentId:int}")]
        public async Task<IActionResult> GetReviewsByIncidentId(int incidentId)
        {
            var result = await _reviewService.GetReviewsByIncidentIdAsync(incidentId);
            return Ok(result);
        }
    }
}
