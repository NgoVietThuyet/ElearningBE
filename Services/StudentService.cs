using ElearningAPI.Data;
using ElearningAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ElearningAPI.Services
{
    public class StudentService : IStudentService
    {
        private readonly AppDbContext _context;

        public StudentService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<object> GetOverviewStats(int studentId)
        {
            var courses = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .ToListAsync();

            var avgProgress = courses.Any() ? courses.Average(e => (double)e.ProgressPercentage) : 0;

            return new
            {
                OverallProgress = (int)Math.Round(avgProgress),
                EnrolledCount = courses.Count,
                CompletedTests = await _context.TestResults.CountAsync(r => r.StudentId == studentId),
                AverageScore = Math.Round(await _context.TestResults
                    .Where(r => r.StudentId == studentId)
                    .AverageAsync(r => (double?)r.Score) ?? 0, 1)
            };
        }

        public async Task<IEnumerable<object>> GetAvailableCourses(int studentId)
        {
            var enrolledCourseIds = _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId);

            return await _context.Courses
                .Include(c => c.Creator)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    CreatorName = c.Creator != null ? c.Creator.FullName : "Admin",
                    LessonCount = c.Lessons.Count,
                    StudentCount = c.Enrollments.Count,
                    IsEnrolled = enrolledCourseIds.Contains(c.Id)
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetMyCourses(int studentId)
        {
            return await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Include(e => e.Course)
                .ThenInclude(c => c.Lessons)
                .Select(e => new
                {
                    e.Course.Id,
                    e.Course.Title,
                    e.Course.Description,
                    Progress = (int)Math.Round(e.ProgressPercentage),
                    TotalLessons = e.Course.Lessons.Count,
                    CompletedLessons = (int)Math.Round((double)e.ProgressPercentage / 100 * e.Course.Lessons.Count),
                    e.EnrolledAt,
                    e.LastAccessed
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetMyLessons(int studentId)
        {
            var enrolledCourseIds = await _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            return await _context.Lessons
                .Where(l => enrolledCourseIds.Contains(l.CourseId))
                .Include(l => l.Course)
                .OrderByDescending(l => l.Id)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.CourseId,
                    CourseTitle = l.Course.Title,
                    Duration = "45 phút",
                    Status = "current"
                })
                .Take(10)
                .ToListAsync();
        }

        public async Task<object?> GetLessonDetail(int studentId, int lessonId)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .Include(l => l.Tests)
                .FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null) return null;

            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == lesson.CourseId);
            if (!isEnrolled) return null;

            return new
            {
                lesson.Id,
                lesson.CourseId,
                CourseTitle = lesson.Course.Title,
                lesson.Title,
                lesson.Description,
                lesson.VideoUrl,
                PdfUrl = lesson.PdfFile != null && lesson.PdfFile.Length > 0 ? $"/api/public/lessons/{lesson.Id}/pdf" : lesson.PdfUrl,
                DocumentUrl = lesson.DocumentFile != null && lesson.DocumentFile.Length > 0 ? $"/api/public/lessons/{lesson.Id}/document" : lesson.DocumentUrl,
                DocumentName = lesson.DocumentFileName ?? lesson.DocumentName,
                Flashcards = lesson.Tests
                    .Where(t => IsContentType(t.Content, "flashcard"))
                    .Select(t => new { t.Id, t.Title, Cards = ReadJsonProperty(t.Content, "cards") }),
                Tests = lesson.Tests
                    .Where(t => IsContentType(t.Content, "quiz"))
                    .Select(t => new { t.Id, t.Title, Questions = ReadJsonProperty(t.Content, "questions") })
            };
        }

        public async Task<object> EnrollCourse(int studentId, int courseId)
        {
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == courseId);
            if (!courseExists) return new { Success = false, Message = "Course not found." };

            var exists = await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == courseId);
            if (!exists)
            {
                _context.Enrollments.Add(new Enrollment
                {
                    StudentId = studentId,
                    CourseId = courseId,
                    ProgressPercentage = 0,
                    EnrolledAt = DateTime.UtcNow,
                    LastAccessed = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }

            return new { Success = true, Message = "Enrolled successfully." };
        }

        public async Task<object?> SubmitTest(int studentId, int testId, IEnumerable<int> answers)
        {
            var test = await _context.Tests
                .Include(t => t.Lesson)
                .FirstOrDefaultAsync(t => t.Id == testId);
            if (test == null || !IsContentType(test.Content, "quiz")) return null;

            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == test.Lesson.CourseId);
            if (!isEnrolled) return null;

            var correctAnswers = ReadCorrectAnswers(test.Content);
            var submitted = answers.ToList();
            var correctCount = correctAnswers
                .Select((correct, index) => index < submitted.Count && submitted[index] == correct)
                .Count(isCorrect => isCorrect);
            var score = correctAnswers.Count == 0 ? 0 : Math.Round((decimal)correctCount / correctAnswers.Count * 10, 2);

            var result = new TestResult
            {
                TestId = testId,
                StudentId = studentId,
                Score = score,
                Status = score >= 5 ? TestStatus.PASSED : TestStatus.FAILED,
                CompletedAt = DateTime.UtcNow
            };
            _context.TestResults.Add(result);

            var enrollment = await _context.Enrollments.FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == test.Lesson.CourseId);
            if (enrollment != null)
            {
                enrollment.LastAccessed = DateTime.UtcNow;
                enrollment.ProgressPercentage = Math.Max(enrollment.ProgressPercentage, 100);
            }

            await _context.SaveChangesAsync();

            return new
            {
                result.Id,
                result.TestId,
                result.Score,
                Status = result.Status.ToString(),
                CorrectCount = correctCount,
                TotalQuestions = correctAnswers.Count,
                result.CompletedAt
            };
        }

        public async Task<IEnumerable<object>> GetTestHistory(int studentId)
        {
            return await _context.TestResults
                .Where(r => r.StudentId == studentId)
                .Include(r => r.Test)
                    .ThenInclude(t => t.Lesson)
                    .ThenInclude(l => l.Course)
                .OrderByDescending(r => r.CompletedAt)
                .Select(r => new
                {
                    r.Id,
                    r.TestId,
                    TestTitle = r.Test.Title,
                    LessonTitle = r.Test.Lesson.Title,
                    CourseTitle = r.Test.Lesson.Course.Title,
                    r.Score,
                    Status = r.Status.ToString(),
                    r.CompletedAt
                })
                .ToListAsync();
        }

        private static bool IsContentType(string content, string type)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                return doc.RootElement.TryGetProperty("type", out var value) && value.GetString() == type;
            }
            catch
            {
                return false;
            }
        }

        private static object ReadJsonProperty(string content, string propertyName)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (!doc.RootElement.TryGetProperty(propertyName, out var value)) return Array.Empty<object>();
                return JsonSerializer.Deserialize<object>(value.GetRawText()) ?? Array.Empty<object>();
            }
            catch
            {
                return Array.Empty<object>();
            }
        }

        private static List<int> ReadCorrectAnswers(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (!doc.RootElement.TryGetProperty("questions", out var questions)) return new List<int>();

                return questions.EnumerateArray()
                    .Select(q => q.TryGetProperty("correctIndex", out var correct) ? correct.GetInt32() : -1)
                    .ToList();
            }
            catch
            {
                return new List<int>();
            }
        }
    }
}
