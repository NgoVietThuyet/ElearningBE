using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElearningAPI.Models
{
    public class Feedback
    {
        [Key]
        public int Id { get; set; }

        public int CourseId { get; set; }
        [ForeignKey("CourseId")]
        public Course Course { get; set; }

        public int TeacherId { get; set; }
        [ForeignKey("TeacherId")]
        public User Teacher { get; set; }

        public int? StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User? Student { get; set; }

        public int? AuthorId { get; set; }
        [ForeignKey("AuthorId")]
        public User? Author { get; set; }

        public int? ParentFeedbackId { get; set; }
        [ForeignKey("ParentFeedbackId")]
        public Feedback? ParentFeedback { get; set; }

        public ICollection<Feedback> Replies { get; set; } = new List<Feedback>();

        [Range(1, 5)]
        public int Rating { get; set; }

        [Required]
        public string Content { get; set; } = string.Empty;

        [MaxLength(50)]
        public string Status { get; set; } = "Đã ghi nhận";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
