using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElearningAPI.Models
{
    public class LessonProgress
    {
        [Key]
        public int Id { get; set; }

        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; }

        public int LessonId { get; set; }
        [ForeignKey("LessonId")]
        public Lesson Lesson { get; set; }

        public int CourseId { get; set; }

        public bool IsCompleted { get; set; } = false;

        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}
