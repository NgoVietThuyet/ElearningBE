using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElearningAPI.Models
{
    public class Enrollment
    {
        [Key]
        public int Id { get; set; }
        
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; }
        
        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; }
        
        public decimal ProgressPercentage { get; set; } = 0.00m;
        
        public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
        public DateTime LastAccessed { get; set; } = DateTime.UtcNow;
    }
}