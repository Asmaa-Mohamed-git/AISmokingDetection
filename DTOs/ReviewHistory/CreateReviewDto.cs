using System.ComponentModel.DataAnnotations;

namespace AISmokingDetection.DTOs.ReviewHistory
{
    // ReviewerId is no longer accepted from the body: it comes from the authenticated user.
    public class CreateReviewDto
    {
        [Required]
        public string Decision { get; set; } = string.Empty;

        [StringLength(500)]
        public string? ReviewReason { get; set; }
    }
}
