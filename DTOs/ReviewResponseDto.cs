using System;

namespace ECommerceApi.DTOs
{
    public class ReviewResponseDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public string? UserName { get; set; }
        public int Rating { get; set; }
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; }
        public int HelpfulCount { get; set; }
        public int NotHelpfulCount { get; set; }
        public int? UserVote { get; set; }
    }
}
