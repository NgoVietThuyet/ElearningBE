using System.ComponentModel.DataAnnotations;

namespace ElearningAPI.Dtos
{
    public class CourseMaterialDto
    {
        [Required]
        public int CourseId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string FileType { get; set; } = "pdf";

        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;
    }

    public class CourseMaterialResponseDto : CourseMaterialDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
