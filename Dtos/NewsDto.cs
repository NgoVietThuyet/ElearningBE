using System.ComponentModel.DataAnnotations;

namespace ElearningAPI.Dtos
{
    public class NewsDto
    {
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        public string Content { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        public int? AuthorId { get; set; }

        [MaxLength(255)]
        public string? AuthorName { get; set; }
    }

    public class NewsResponseDto : NewsDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
