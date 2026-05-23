using System.Collections.Generic;

namespace ElearningAPI.Dtos
{
    public class ParsedQuestionDto
    {
        public string Question { get; set; } = string.Empty;
        public List<string> Options { get; set; } = new();
        public int Answer { get; set; } = 0; // Index of correct option (0-3)
        public int CorrectIndex { get; set; } = 0; // Duplicated for compatibility
    }

    public class ParsedQuizDto
    {
        public string Title { get; set; } = "Bài kiểm tra trắc nghiệm";
        public List<ParsedQuestionDto> Questions { get; set; } = new();
        public int DurationMinutes { get; set; } = 30; // Default duration
    }
}
