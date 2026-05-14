using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElearningAPI.Models
{
    public class CourseMaterial
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int CourseId { get; set; }

        [ForeignKey("CourseId")]
        public Course Course { get; set; } = null!;

        [Required, MaxLength(255)]
        public string Title { get; set; } = string.Empty;

        [Required, MaxLength(500)]
        public string FileUrl { get; set; } = string.Empty;

        [Required, MaxLength(20)]
        public string FileType { get; set; } = "pdf";

        [MaxLength(100)]
        public string MimeType { get; set; } = string.Empty;

        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
