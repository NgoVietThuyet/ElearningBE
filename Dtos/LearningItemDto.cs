using System.ComponentModel.DataAnnotations;

namespace ElearningAPI.Dtos
{
    public class LearningItemDto
    {
        [Required]
        public int CourseId { get; set; }

        [Required]
        public int LessonId { get; set; }

        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string Type { get; set; } = "quiz";

        [Required]
        public string Content { get; set; } = "{}";
    }

    public class LearningItemResponseDto : LearningItemDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
