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

        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        public int TeachingExperienceYears { get; set; } = 0;

        [MaxLength(2000)]
        public string? ShortBio { get; set; }

        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        
        public string? AvatarUrl { get; set; }

        public byte[]? AvatarImage { get; set; }

        public string? AvatarContentType { get; set; }

        public string? AvatarFileName { get; set; }

        // Navigation properties (Mối quan hệ)
        public ICollection<Course> CreatedCourses { get; set; }
        public ICollection<Lesson> CreatedLessons { get; set; }
        public ICollection<News> AuthoredNews { get; set; }
        public ICollection<TeacherStudent> StudentsManaged { get; set; } 
        public ICollection<TeacherStudent> TeachersManagingMe { get; set; }
    }
}
