using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElearningAPI.Models
{
    public enum TestStatus { PASSED, FAILED, IN_PROGRESS }

    public class TestResult
    {
        [Key]
        public int Id { get; set; }
        
        public int TestId { get; set; }
        [ForeignKey("TestId")]
        public Test Test { get; set; }
        
        public int StudentId { get; set; }
        [ForeignKey("StudentId")]
        public User Student { get; set; }
        
        public decimal Score { get; set; }
        
        public TestStatus Status { get; set; } = TestStatus.IN_PROGRESS;
        
        public DateTime CompletedAt { get; set; } = DateTime.UtcNow;
    }
}