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
                    .AverageAsync(r => (decimal?)r.Score) ?? 0m, 1)
            };
        }

        public async Task<IEnumerable<object>> GetAvailableCourses(int studentId)
        {
            var enrolledCourseIds = _context.Enrollments
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId);

            return await _context.Courses
                .AsNoTracking()
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
            var enrollments = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId)
                .Select(e => new
                {
                    e.Course.Id,
                    e.Course.Title,
                    e.Course.Description,
                    ProgressPercentage = e.ProgressPercentage,
                    TotalLessons = e.Course.Lessons.Count,
                    SortOrder = e.Course.SortOrder,
                    e.EnrolledAt,
                    e.LastAccessed
                })
                .ToListAsync();

            var courseIds = enrollments.Select(e => e.Id).ToList();
            var lessonProgresses = await _context.LessonProgresses
                .AsNoTracking()
                .Where(lp => lp.StudentId == studentId && courseIds.Contains(lp.CourseId) && lp.IsCompleted)
                .GroupBy(lp => lp.CourseId)
                .Select(g => new { CourseId = g.Key, Count = g.Count() })
                .ToListAsync();

            var progressMap = lessonProgresses.ToDictionary(p => p.CourseId, p => p.Count);

            return enrollments.Select(e =>
            {
                var completedCount = progressMap.GetValueOrDefault(e.Id, 0);
                var dynamicProgress = e.TotalLessons > 0 ? (int)Math.Round((double)completedCount / e.TotalLessons * 100) : 0;
                return new
                {
                    e.Id,
                    e.Title,
                    e.Description,
                    Progress = dynamicProgress,
                    e.TotalLessons,
                    CompletedLessons = completedCount,
                    SortOrder = e.SortOrder,
                    e.EnrolledAt,
                    e.LastAccessed
                };
            }).ToList();
        }

        public async Task<IEnumerable<object>> GetMyLessons(int studentId)
        {
            var enrolledCourseIds = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.StudentId == studentId)
                .Select(e => e.CourseId)
                .ToListAsync();

            return await _context.Lessons
                .AsNoTracking()
                .Where(l => enrolledCourseIds.Contains(l.CourseId))
                .OrderByDescending(l => l.Id)
                .Select(l => new
                {
                    l.Id,
                    l.Title,
                    l.CourseId,
                    CourseTitle = l.Course.Title,
                    l.QuizUrl,
                    Duration = "45 phút",
                    Status = "current"
                })
                .Take(10)
                .ToListAsync();
        }

        public async Task<object?> GetLessonDetail(int studentId, int lessonId, bool isAdmin = false)
        {
            var lesson = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.Id == lessonId)
                .Select(l => new
                {
                    l.Id,
                    l.CourseId,
                    CourseTitle = l.Course.Title,
                    l.Title,
                    l.Description,
                    l.VideoUrl,
                    l.PdfUrl,
                    l.PdfFileName,
                    l.PdfContentType,
                    l.DocumentUrl,
                    l.DocumentName,
                    l.DocumentFileName,
                    l.DocumentContentType,
                    l.SlideFileName,
                    l.SlideContentType,
                    l.LessonPlanFileName,
                    l.LessonPlanContentType,
                    l.ArVrUrl,
                    l.QuizUrl,
                    Tests = l.Tests.Select(t => new { t.Id, t.Title, t.Content }).ToList()
                })
                .FirstOrDefaultAsync();
            if (lesson == null) return null;

            if (!isAdmin)
            {
                var isEnrolled = await _context.Enrollments.AnyAsync(e => e.StudentId == studentId && e.CourseId == lesson.CourseId);
                if (!isEnrolled) return null;
            }

            return new
            {
                lesson.Id,
                lesson.CourseId,
                lesson.CourseTitle,
                lesson.Title,
                lesson.Description,
                lesson.VideoUrl,
                PdfUrl = !string.IsNullOrWhiteSpace(lesson.PdfFileName) || !string.IsNullOrWhiteSpace(lesson.PdfContentType) ? $"/api/public/lessons/{lesson.Id}/pdf" : lesson.PdfUrl,
                DocumentUrl = !string.IsNullOrWhiteSpace(lesson.DocumentFileName) || !string.IsNullOrWhiteSpace(lesson.DocumentContentType) ? $"/api/public/lessons/{lesson.Id}/document" : lesson.DocumentUrl,
                DocumentName = lesson.DocumentFileName ?? lesson.DocumentName,
                SlideUrl = !string.IsNullOrWhiteSpace(lesson.SlideFileName) || !string.IsNullOrWhiteSpace(lesson.SlideContentType) ? $"/api/public/lessons/{lesson.Id}/slide" : null,
                SlideFileName = lesson.SlideFileName,
                LessonPlanUrl = !string.IsNullOrWhiteSpace(lesson.LessonPlanFileName) || !string.IsNullOrWhiteSpace(lesson.LessonPlanContentType) ? $"/api/public/lessons/{lesson.Id}/lesson-plan" : null,
                LessonPlanFileName = lesson.LessonPlanFileName,
                lesson.ArVrUrl,
                lesson.QuizUrl,
                Flashcards = lesson.Tests
                    .Where(t => IsContentType(t.Content, "flashcard"))
                    .Select(t => new { t.Id, t.Title, Cards = ReadJsonProperty(t.Content, "cards") }),
                Tests = lesson.Tests
                    .Where(t => IsContentType(t.Content, "quiz"))
                    .Select(t => new { 
                        t.Id, 
                        t.Title, 
                        Questions = ReadJsonProperty(t.Content, "questions"),
                        DurationMinutes = ReadJsonIntProperty(t.Content, "durationMinutes"),
                        EndTime = ReadJsonStringProperty(t.Content, "endTime")
                    }),
                Exercises = lesson.Tests
                    .Where(t => IsContentType(t.Content, "exam"))
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

            await _context.SaveChangesAsync();

            // Mark the lesson as completed (progress recalculated inside CompleteLesson)
            await CompleteLesson(studentId, test.LessonId);

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

        public async Task<bool> CompleteLesson(int studentId, int lessonId)
        {
            var lesson = await _context.Lessons
                .AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == lessonId);
            if (lesson == null) return false;

            var existing = await _context.LessonProgresses
                .FirstOrDefaultAsync(lp => lp.StudentId == studentId && lp.LessonId == lessonId);

            if (existing == null)
            {
                _context.LessonProgresses.Add(new LessonProgress
                {
                    StudentId = studentId,
                    LessonId = lessonId,
                    CourseId = lesson.CourseId,
                    IsCompleted = true,
                    CompletedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
            }
            else if (!existing.IsCompleted)
            {
                existing.IsCompleted = true;
                existing.CompletedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            var totalLessons = await _context.Lessons.CountAsync(l => l.CourseId == lesson.CourseId);
            var completedCount = await _context.LessonProgresses
                .CountAsync(lp => lp.StudentId == studentId && lp.CourseId == lesson.CourseId && lp.IsCompleted);

            var enrollment = await _context.Enrollments
                .FirstOrDefaultAsync(e => e.StudentId == studentId && e.CourseId == lesson.CourseId);

            if (enrollment != null)
            {
                enrollment.LastAccessed = DateTime.UtcNow;
                if (totalLessons > 0)
                {
                    var newProgress = (int)Math.Round((double)completedCount / totalLessons * 100);
                    enrollment.ProgressPercentage = Math.Max(enrollment.ProgressPercentage, newProgress);
                }
                await _context.SaveChangesAsync();
            }

            return true;
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
                    .Select(q => q.TryGetProperty("correctIndex", out var correct) ? correct.GetInt32() : (q.TryGetProperty("answer", out var ans) ? ans.GetInt32() : -1))
                    .ToList();
            }
            catch
            {
                return new List<int>();
            }
        }

        private static int ReadJsonIntProperty(string content, string propertyName)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (!doc.RootElement.TryGetProperty(propertyName, out var value)) return 0;
                return value.GetInt32();
            }
            catch
            {
                return 0;
            }
        }

        private static string ReadJsonStringProperty(string content, string propertyName)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (!doc.RootElement.TryGetProperty(propertyName, out var value)) return string.Empty;
                return value.GetString() ?? string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
