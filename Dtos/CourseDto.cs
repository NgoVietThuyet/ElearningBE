using System.ComponentModel.DataAnnotations;

namespace ElearningAPI.Dtos
{
    public class CourseDto
    {
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }
        
        public int? TeacherId { get; set; }
    }

    public class CourseResponseDto : CourseDto
    {
        public int Id { get; set; }
        public int CreatedBy { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
