using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace ElearningAPI.Dtos
{
    public class LessonDto
    {
        [Required]
        public int CourseId { get; set; }
        
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        [MaxLength(255)]
        public string? VideoUrl { get; set; }
        
        [MaxLength(255)]
        public string? PdfUrl { get; set; }

        [MaxLength(500)]
        public string? DocumentUrl { get; set; }

        [MaxLength(255)]
        public string? DocumentName { get; set; }

        public IFormFile? PdfFile { get; set; }

        public IFormFile? DocumentFile { get; set; }

        public IFormFile? LessonPlanFile { get; set; }

        public IFormFile? SlideFile { get; set; }

        [MaxLength(500)]
        public string? ArVrUrl { get; set; }

        [MaxLength(500)]
        public string? QuizUrl { get; set; }
    }

    public class LessonResponseDto : LessonDto
    {
        public int Id { get; set; }
        public int CreatedBy { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
