using System;

namespace ElearningAPI.Models
{
    public class TeacherStudent
    {
        public int TeacherId { get; set; }
        public User Teacher { get; set; }
        
        public int StudentId { get; set; }
        public User Student { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}