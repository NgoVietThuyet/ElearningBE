using System.ComponentModel.DataAnnotations;

namespace ElearningAPI.Dtos
{
    public class RegisterDto {
        [Required]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Password { get; set; } = string.Empty;
       
    }
}