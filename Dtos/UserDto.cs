using System;
using System.ComponentModel.DataAnnotations;
using ElearningAPI.Models;

namespace ElearningAPI.Dtos
{
    public class UserResponseDto
    {
        public int Id { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateUserDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        [EmailAddress]
        [MaxLength(100)]
        public string Email { get; set; }

        [Required]
        [MinLength(6)]
        public string Password { get; set; }

        [Required]
        public UserRole Role { get; set; }
    }

    public class UpdateUserDto
    {
        [Required]
        [MaxLength(100)]
        public string FullName { get; set; }

        [Required]
        public UserRole Role { get; set; }
    }
}
