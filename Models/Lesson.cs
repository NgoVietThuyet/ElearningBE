using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElearningAPI.Models
{
    public class Lesson
    {
        [Key]
        public int Id { get; set; }
        
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; }
        
        [Required, MaxLength(255)]
        public string Title { get; set; }
        
        public string Description { get; set; }
        
        [MaxLength(255)]
        public string? VideoUrl { get; set; }
        
        [MaxLength(255)]
        public string? PdfUrl { get; set; }

        [MaxLength(500)]
        public string? DocumentUrl { get; set; }

        [MaxLength(255)]
        public string? DocumentName { get; set; }

        public byte[]? PdfFile { get; set; }

        [MaxLength(120)]
        public string? PdfContentType { get; set; }

        [MaxLength(255)]
        public string? PdfFileName { get; set; }

        public byte[]? DocumentFile { get; set; }

        [MaxLength(120)]
        public string? DocumentContentType { get; set; }

        [MaxLength(255)]
        public string? DocumentFileName { get; set; }

        public byte[]? LessonPlanFile { get; set; }

        [MaxLength(120)]
        public string? LessonPlanContentType { get; set; }

        [MaxLength(255)]
        public string? LessonPlanFileName { get; set; }

        public byte[]? SlideFile { get; set; }

        [MaxLength(120)]
        public string? SlideContentType { get; set; }

        [MaxLength(255)]
        public string? SlideFileName { get; set; }

        [MaxLength(500)]
        public string? ArVrUrl { get; set; }
        
        public int CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public User Creator { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Test> Tests { get; set; }
    }
}
