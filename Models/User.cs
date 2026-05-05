using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ElearningAPI.Models
{
    public enum UserRole
    {
        ADMIN,
        TEACHER,
        STUDENT
    }

    public class User
    {
        [Key]
        public int Id { get; set; }
        
        [Required, MaxLength(100)]
        public string FullName { get; set; }
        
        [Required, MaxLength(100)]
        public string Email { get; set; }
        
        [Required, MaxLength(255)]
        public string PasswordHash { get; set; }
        
        [Required]
        public UserRole Role { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties (Mối quan hệ)
        public ICollection<Course> CreatedCourses { get; set; }
        public ICollection<Lesson> CreatedLessons { get; set; }
        public ICollection<News> AuthoredNews { get; set; }
        public ICollection<TeacherStudent> StudentsManaged { get; set; } 
        public ICollection<TeacherStudent> TeachersManagingMe { get; set; }
    }
}