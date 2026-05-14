using System;
using System.ComponentModel.DataAnnotations;
using ElearningAPI.Models;
using Microsoft.AspNetCore.Http;

namespace ElearningAPI.Dtos
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string? AvatarImageDataUrl { get; set; }
        public string? AvatarContentType { get; set; }
        public string? AvatarFileName { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public int TeachingExperienceYears { get; set; }
        public string? ShortBio { get; set; }
        public bool IsActive { get; set; }
        public int? AssignedCourseId { get; set; }
        public string? AssignedCourseTitle { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateUserDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; } = string.Empty;

        [Required]
        [MinLength(6)]
        public string Password { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [Range(0, 99)]
        public int TeachingExperienceYears { get; set; } = 0;

        [MaxLength(2000)]
        public string? ShortBio { get; set; }

        public bool IsActive { get; set; } = true;

        public int? AssignedCourseId { get; set; }

        public string? AvatarUrl { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }

    public class UpdateUserDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        public UserRole Role { get; set; }

        public DateOnly? DateOfBirth { get; set; }

        [MaxLength(20)]
        public string? Gender { get; set; }

        [MaxLength(30)]
        public string? PhoneNumber { get; set; }

        [MaxLength(255)]
        public string? Address { get; set; }

        [Range(0, 99)]
        public int TeachingExperienceYears { get; set; } = 0;

        [MaxLength(2000)]
        public string? ShortBio { get; set; }

        public bool IsActive { get; set; } = true;

        public int? AssignedCourseId { get; set; }

        public string? AvatarUrl { get; set; }

        public IFormFile? AvatarFile { get; set; }
    }
}
