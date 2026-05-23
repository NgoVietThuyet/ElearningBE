using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElearningAPI.Models
{
    public class Course
    {
        [Key]
        public int Id { get; set; }
        
        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public string? AvatarUrl { get; set; }

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        public string? IntroVideoUrl { get; set; }

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
        
        public int CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }
        
        public int? TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public int SortOrder { get; set; } = 0;

        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
        public ICollection<CourseMaterial> Materials { get; set; } = new List<CourseMaterial>();
    }
}
