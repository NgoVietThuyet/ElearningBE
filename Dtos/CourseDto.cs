using System.ComponentModel.DataAnnotations;

namespace ElearningAPI.Dtos
{
    public class CourseDto
    {
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;

        public string? AvatarUrl { get; set; }

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        public string? IntroVideoUrl { get; set; }
        
        public int? TeacherId { get; set; }

        [MaxLength(100)]
        public string Category { get; set; } = "Sinh học";

        [MaxLength(30)]
        public string Status { get; set; } = "Published";

        [MaxLength(50)]
        public string Level { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Language { get; set; } = "Tiếng Việt";

        public int DurationMinutes { get; set; } = 0;

        public int ExpectedStudentCount { get; set; } = 0;

        public DateTime? StartDate { get; set; }

        public DateTime? EndDate { get; set; }

        public string LearningOutcomes { get; set; } = string.Empty;
    }

    public class CourseResponseDto : CourseDto
    {
        public int Id { get; set; }
        public int CreatedBy { get; set; }
        public string CreatorName { get; set; } = string.Empty;
        public string TeacherName { get; set; } = string.Empty;
        public string? TeacherAvatarUrl { get; set; }
        public int LessonCount { get; set; }
        public int StudentCount { get; set; }
        public decimal AverageProgress { get; set; }
        public int SortOrder { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
