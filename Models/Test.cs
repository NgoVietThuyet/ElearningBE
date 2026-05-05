using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElearningAPI.Models
{
    public class Test
    {
        [Key]
        public int Id { get; set; }
        
        public int LessonId { get; set; }
        [ForeignKey("LessonId")]
        public Lesson Lesson { get; set; }
        
        [Required, MaxLength(255)]
        public string Title { get; set; }
        
        [Column(TypeName = "jsonb")] // Định dạng tối ưu của PostgreSQL cho dữ liệu JSON
        public string Content { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public ICollection<TestResult> TestResults { get; set; }
    }
}