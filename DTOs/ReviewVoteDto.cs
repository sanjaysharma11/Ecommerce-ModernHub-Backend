using System.ComponentModel.DataAnnotations;

namespace ECommerceApi.DTOs
{
    public class ReviewVoteDto
    {
        [Required]
        public bool IsHelpful { get; set; }
    }
}
