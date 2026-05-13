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
        
        public int CreatedBy { get; set; }
        [ForeignKey("CreatedBy")]
        public User? Creator { get; set; }
        
        public int? TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public User? Teacher { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
        public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    }
}