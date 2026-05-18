using ElearningAPI.Data;
using ElearningAPI.Dtos;
using ElearningAPI.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace ElearningAPI.Services
{
    public class TeacherService : ITeacherService
    {
        private readonly AppDbContext _context;
        private static readonly HashSet<string> AllowedPdfTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf"
        };
        private static readonly HashSet<string> AllowedDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            "application/vnd.ms-powerpoint",
            "application/vnd.openxmlformats-officedocument.presentationml.presentation",
            "application/zip",
            "application/x-zip-compressed",
            "application/octet-stream"
        };

        public TeacherService(AppDbContext context)
        {
            _context = context;
        }

        private static async Task<byte[]> ReadFormFileAsync(Microsoft.AspNetCore.Http.IFormFile file)
        {
            await using var stream = file.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            return memory.ToArray();
        }

        private static async Task ApplyLessonFilesAsync(Lesson lesson, LessonDto dto)
        {
            if (dto.PdfFile is { Length: > 0 })
            {
                if (!AllowedPdfTypes.Contains(dto.PdfFile.ContentType))
                    throw new InvalidOperationException("Tệp PDF không hợp lệ.");

                lesson.PdfFile = await ReadFormFileAsync(dto.PdfFile);
                lesson.PdfContentType = dto.PdfFile.ContentType;
                lesson.PdfFileName = Path.GetFileName(dto.PdfFile.FileName);
                lesson.PdfUrl = string.Empty;
            }

            if (dto.DocumentFile is { Length: > 0 })
            {
                if (!AllowedDocumentTypes.Contains(dto.DocumentFile.ContentType) && !AllowedPdfTypes.Contains(dto.DocumentFile.ContentType))
                    throw new InvalidOperationException("Tệp Word không hợp lệ. Chỉ hỗ trợ DOC hoặc DOCX.");

                lesson.DocumentFile = await ReadFormFileAsync(dto.DocumentFile);
                lesson.DocumentContentType = dto.DocumentFile.ContentType;
                lesson.DocumentFileName = Path.GetFileName(dto.DocumentFile.FileName);
                lesson.DocumentName = lesson.DocumentFileName;
                lesson.DocumentUrl = null;
            }

            if (dto.LessonPlanFile is { Length: > 0 })
            {
                if (!AllowedDocumentTypes.Contains(dto.LessonPlanFile.ContentType) && !AllowedPdfTypes.Contains(dto.LessonPlanFile.ContentType))
                    throw new InvalidOperationException("Tep giao an khong hop le.");

                lesson.LessonPlanFile = await ReadFormFileAsync(dto.LessonPlanFile);
                lesson.LessonPlanContentType = dto.LessonPlanFile.ContentType;
                lesson.LessonPlanFileName = Path.GetFileName(dto.LessonPlanFile.FileName);
            }

            if (dto.SlideFile is { Length: > 0 })
            {
                if (!AllowedDocumentTypes.Contains(dto.SlideFile.ContentType) && !AllowedPdfTypes.Contains(dto.SlideFile.ContentType))
                    throw new InvalidOperationException("Tep slide khong hop le.");

                lesson.SlideFile = await ReadFormFileAsync(dto.SlideFile);
                lesson.SlideContentType = dto.SlideFile.ContentType;
                lesson.SlideFileName = Path.GetFileName(dto.SlideFile.FileName);
            }

        }

        private static string ResolveLessonPdfUrl(Lesson lesson)
        {
            if (lesson.PdfFile != null && lesson.PdfFile.Length > 0)
                return $"/api/public/lessons/{lesson.Id}/pdf";

            return lesson.PdfUrl ?? string.Empty;
        }

        private static string? ResolveLessonDocumentUrl(Lesson lesson)
        {
            if (lesson.DocumentFile != null && lesson.DocumentFile.Length > 0)
                return $"/api/public/lessons/{lesson.Id}/document";

            return lesson.DocumentUrl;
        }

        public async Task<object> GetOverviewStats(int teacherId)
        {
            var studentIds = ManagedStudentIdsQuery(teacherId);
            var courseIds = TeacherCourseIdsQuery(teacherId);

            var studentCount = await studentIds.CountAsync();
            var courseCount = await courseIds.CountAsync();
            var lessonCount = await _context.Lessons.CountAsync(l => l.CreatedBy == teacherId);
            var assessmentCount = await _context.Tests.CountAsync(t => t.Lesson.CreatedBy == teacherId || courseIds.Contains(t.Lesson.CourseId));
            var avgProgress = await _context.Enrollments
                .Where(e => studentIds.Contains(e.StudentId) && courseIds.Contains(e.CourseId))
                .AverageAsync(e => (double?)e.ProgressPercentage) ?? 0;
            var avgScore = await TeacherTestResultsQuery(teacherId)
                .AverageAsync(r => (double?)r.Score) ?? 0;

            return new
            {
                StudentCount = studentCount,
                CourseCount = courseCount,
                LessonCount = lessonCount,
                AssessmentCount = assessmentCount,
                CompletionRate = $"{Math.Round(avgProgress, 1)}%",
                AvgScore = Math.Round(avgScore, 1).ToString("F1")
            };
        }

        public async Task<IEnumerable<object>> GetMyCourses(int teacherId)
        {
            return await _context.Courses
                .Where(c => c.CreatedBy == teacherId || c.TeacherId == teacherId)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    c.Description,
                    c.AvatarUrl,
                    c.Category,
                    c.Level,
                    c.DurationMinutes,
                    c.CreatedAt,
                    LessonCount = c.Lessons.Count,
                    StudentCount = c.Enrollments.Count,
                    AvgProgress = Math.Round(c.Enrollments.Average(e => (double?)e.ProgressPercentage) ?? 0, 1)
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetMyStudents(int teacherId)
        {
            var studentIds = ManagedStudentIdsQuery(teacherId);
            var courseIds = TeacherCourseIdsQuery(teacherId);

            return await _context.TeacherStudents
                .Where(ts => ts.TeacherId == teacherId)
                .Include(ts => ts.Student)
                .Select(ts => new
                {
                    ts.Student.Id,
                    ts.Student.FullName,
                    ts.Student.Email,
                    Progress = Math.Round(_context.Enrollments
                        .Where(e => e.StudentId == ts.StudentId && courseIds.Contains(e.CourseId))
                        .Average(e => (double?)e.ProgressPercentage) ?? 0, 1),
                    CourseCount = _context.Enrollments.Count(e => e.StudentId == ts.StudentId && courseIds.Contains(e.CourseId)),
                    TestCount = _context.TestResults.Count(r => r.StudentId == ts.StudentId && _context.Tests.Any(t => t.Id == r.TestId && t.Lesson.CreatedBy == teacherId)),
                    AvgScore = Math.Round(_context.TestResults
                        .Where(r => r.StudentId == ts.StudentId && _context.Tests.Any(t => t.Id == r.TestId && t.Lesson.CreatedBy == teacherId))
                        .Average(r => (double?)r.Score) ?? 0, 1),
                    Status = "Dang hoc",
                    ts.CreatedAt
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();
        }

        public async Task<object?> GetStudentDetail(int teacherId, int studentId)
        {
            var isManaged = await _context.TeacherStudents.AnyAsync(ts => ts.TeacherId == teacherId && ts.StudentId == studentId);
            if (!isManaged) return null;

            var student = await _context.Users
                .Where(u => u.Id == studentId && u.Role == UserRole.STUDENT)
                .Select(u => new { u.Id, u.FullName, u.Email, u.CreatedAt })
                .FirstOrDefaultAsync();
            if (student == null) return null;

            var courseIds = TeacherCourseIdsQuery(teacherId);
            var progress = await _context.Enrollments
                .Where(e => e.StudentId == studentId && courseIds.Contains(e.CourseId))
                .Include(e => e.Course)
                .Select(e => new
                {
                    e.CourseId,
                    CourseTitle = e.Course.Title,
                    Progress = e.ProgressPercentage,
                    e.EnrolledAt,
                    e.LastAccessed
                })
                .ToListAsync();

            var tests = await TeacherTestResultsQuery(teacherId)
                .Where(r => r.StudentId == studentId)
                .Select(r => new
                {
                    r.TestId,
                    TestTitle = r.Test.Title,
                    LessonTitle = r.Test.Lesson.Title,
                    CourseTitle = r.Test.Lesson.Course.Title,
                    r.Score,
                    Status = r.Status.ToString(),
                    r.CompletedAt
                })
                .ToListAsync();

            return new
            {
                student.Id,
                student.FullName,
                student.Email,
                student.CreatedAt,
                Courses = progress,
                TestResults = tests
            };
        }

        public async Task<IEnumerable<object>> GetMyLessons(int teacherId)
        {
            var courseIds = TeacherCourseIdsQuery(teacherId);
            return await _context.Lessons
                .AsNoTracking()
                .Where(l => l.CreatedBy == teacherId || courseIds.Contains(l.CourseId))
                .OrderByDescending(l => l.UpdatedAt)
                .Select(l => new
                {
                    l.Id,
                    l.CourseId,
                    CourseTitle = l.Course.Title,
                    l.Title,
                    l.Description,
                    l.VideoUrl,
                    PdfUrl = !string.IsNullOrWhiteSpace(l.PdfFileName) || !string.IsNullOrWhiteSpace(l.PdfContentType) ? $"/api/public/lessons/{l.Id}/pdf" : l.PdfUrl,
                    DocumentUrl = !string.IsNullOrWhiteSpace(l.DocumentFileName) || !string.IsNullOrWhiteSpace(l.DocumentContentType) ? $"/api/public/lessons/{l.Id}/document" : l.DocumentUrl,
                    DocumentName = l.DocumentFileName ?? l.DocumentName,
                    LessonPlanUrl = !string.IsNullOrWhiteSpace(l.LessonPlanFileName) || !string.IsNullOrWhiteSpace(l.LessonPlanContentType) ? $"/api/public/lessons/{l.Id}/lesson-plan" : null,
                    l.LessonPlanFileName,
                    SlideUrl = !string.IsNullOrWhiteSpace(l.SlideFileName) || !string.IsNullOrWhiteSpace(l.SlideContentType) ? $"/api/public/lessons/{l.Id}/slide" : null,
                    l.SlideFileName,
                    l.ArVrUrl,
                    l.QuizUrl,
                    QuizCount = string.IsNullOrWhiteSpace(l.QuizUrl) ? 0 : 1,
                    StudentCount = _context.Enrollments.Count(e => e.CourseId == l.CourseId),
                    Progress = Math.Round(_context.Enrollments
                        .Where(e => e.CourseId == l.CourseId)
                        .Average(e => (double?)e.ProgressPercentage) ?? 0, 1),
                    Date = l.CreatedAt.ToString("yyyy-MM-dd"),
                    l.CreatedAt,
                    l.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetMyFeedbacks(int teacherId)
        {
            var students = await _context.TeacherStudents
                .Where(ts => ts.TeacherId == teacherId)
                .Include(ts => ts.Student)
                .Take(5)
                .ToListAsync();

            return students.Select(ts => new
            {
                Id = ts.StudentId,
                Student = ts.Student.FullName,
                Course = "Khoa hoc cua toi",
                Content = "Noi dung bai giang de hieu va truc quan.",
                Date = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd")
            });
        }

        public async Task<bool> AddStudentToClass(int teacherId, string studentEmail)
        {
            var email = studentEmail.Trim().ToLower();
            var student = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Role == UserRole.STUDENT);
            if (student == null) return false;

            var exists = await _context.TeacherStudents.AnyAsync(ts => ts.TeacherId == teacherId && ts.StudentId == student.Id);
            if (exists) return true;

            _context.TeacherStudents.Add(new TeacherStudent
            {
                TeacherId = teacherId,
                StudentId = student.Id,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<object?> UpdateStudentAsync(int teacherId, int studentId, string fullName)
        {
            var isManaged = await _context.TeacherStudents.AnyAsync(ts => ts.TeacherId == teacherId && ts.StudentId == studentId);
            if (!isManaged) return null;

            var student = await _context.Users.FirstOrDefaultAsync(u => u.Id == studentId && u.Role == UserRole.STUDENT);
            if (student == null) return null;

            student.FullName = fullName.Trim();
            student.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new { student.Id, student.FullName, student.Email, student.CreatedAt };
        }

        public async Task<bool> RemoveStudentFromClass(int teacherId, int studentId)
        {
            var link = await _context.TeacherStudents.FirstOrDefaultAsync(ts => ts.TeacherId == teacherId && ts.StudentId == studentId);
            if (link == null) return false;

            _context.TeacherStudents.Remove(link);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<object?> CreateLessonAsync(int teacherId, LessonDto dto)
        {
            var ownsCourse = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId && (c.CreatedBy == teacherId || c.TeacherId == teacherId));
            if (!ownsCourse) return null;

            var lesson = new Lesson
            {
                CourseId = dto.CourseId,
                Title = dto.Title,
                Description = dto.Description,
                VideoUrl = dto.VideoUrl ?? string.Empty,
                PdfUrl = dto.PdfUrl ?? string.Empty,
                DocumentUrl = dto.DocumentUrl,
                DocumentName = dto.DocumentName,
                ArVrUrl = dto.ArVrUrl,
                QuizUrl = dto.QuizUrl,
                CreatedBy = teacherId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await ApplyLessonFilesAsync(lesson, dto);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();
            return await GetLessonProjection(teacherId, lesson.Id);
        }

        public async Task<object?> UpdateLessonAsync(int teacherId, int lessonId, LessonDto dto)
        {
            var courseIds = TeacherCourseIdsQuery(teacherId);
            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId && (l.CreatedBy == teacherId || courseIds.Contains(l.CourseId)));
            if (lesson == null) return null;

            var ownsCourse = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId && (c.CreatedBy == teacherId || c.TeacherId == teacherId));
            if (!ownsCourse) return null;

            lesson.CourseId = dto.CourseId;
            lesson.Title = dto.Title;
            lesson.Description = dto.Description;
            lesson.VideoUrl = dto.VideoUrl ?? string.Empty;
            lesson.PdfUrl = dto.PdfUrl ?? string.Empty;
            lesson.DocumentUrl = dto.DocumentUrl;
            lesson.DocumentName = dto.DocumentName;
            lesson.ArVrUrl = dto.ArVrUrl;
            lesson.QuizUrl = dto.QuizUrl;
            lesson.UpdatedAt = DateTime.UtcNow;

            await ApplyLessonFilesAsync(lesson, dto);
            await _context.SaveChangesAsync();
            return await GetLessonProjection(teacherId, lesson.Id);
        }

        public async Task<bool> DeleteLessonAsync(int teacherId, int lessonId)
        {
            var courseIds = TeacherCourseIdsQuery(teacherId);
            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId && (l.CreatedBy == teacherId || courseIds.Contains(l.CourseId)));
            if (lesson == null) return false;

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<object>> GetLessonLearningItems(int teacherId, int lessonId)
        {
            var courseIds = TeacherCourseIdsQuery(teacherId);
            var ownsLesson = await _context.Lessons.AnyAsync(l => l.Id == lessonId && (l.CreatedBy == teacherId || courseIds.Contains(l.CourseId)));
            if (!ownsLesson) return Enumerable.Empty<object>();

            return await _context.Tests
                .Where(t => t.LessonId == lessonId)
                .OrderByDescending(t => t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.LessonId,
                    t.Title,
                    Type = GetContentType(t.Content),
                    t.Content,
                    t.CreatedAt
                })
                .ToListAsync();
        }

        public async Task<object?> CreateLearningItem(int teacherId, int lessonId, string title, string content)
        {
            var courseIds = TeacherCourseIdsQuery(teacherId);
            var ownsLesson = await _context.Lessons.AnyAsync(l => l.Id == lessonId && (l.CreatedBy == teacherId || courseIds.Contains(l.CourseId)));
            if (!ownsLesson || !IsValidLearningContent(content)) return null;

            var item = new Test
            {
                LessonId = lessonId,
                Title = title.Trim(),
                Content = content,
                CreatedAt = DateTime.UtcNow
            };
            _context.Tests.Add(item);
            await _context.SaveChangesAsync();

            return new { item.Id, item.LessonId, item.Title, Type = GetContentType(item.Content), item.Content, item.CreatedAt };
        }

        public async Task<object?> UpdateLearningItem(int teacherId, int testId, string title, string content)
        {
            var courseIds = TeacherCourseIdsQuery(teacherId);
            var item = await _context.Tests
                .Include(t => t.Lesson)
                .FirstOrDefaultAsync(t => t.Id == testId && (t.Lesson.CreatedBy == teacherId || courseIds.Contains(t.Lesson.CourseId)));
            if (item == null || !IsValidLearningContent(content)) return null;

            item.Title = title.Trim();
            item.Content = content;
            await _context.SaveChangesAsync();

            return new { item.Id, item.LessonId, item.Title, Type = GetContentType(item.Content), item.Content, item.CreatedAt };
        }

        public async Task<bool> DeleteLearningItem(int teacherId, int testId)
        {
            var courseIds = TeacherCourseIdsQuery(teacherId);
            var item = await _context.Tests
                .Include(t => t.Lesson)
                .FirstOrDefaultAsync(t => t.Id == testId && (t.Lesson.CreatedBy == teacherId || courseIds.Contains(t.Lesson.CourseId)));
            if (item == null) return false;

            _context.Tests.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<object>> GetAcademicProgress(int teacherId)
        {
            var studentIds = ManagedStudentIdsQuery(teacherId);
            var courseIds = TeacherCourseIdsQuery(teacherId);

            return await _context.Enrollments
                .Where(e => studentIds.Contains(e.StudentId) && courseIds.Contains(e.CourseId))
                .Include(e => e.Student)
                .Include(e => e.Course)
                .OrderBy(e => e.Student.FullName)
                .ThenBy(e => e.Course.Title)
                .Select(e => new
                {
                    StudentId = e.StudentId,
                    StudentName = e.Student.FullName,
                    e.Student.Email,
                    CourseId = e.CourseId,
                    CourseTitle = e.Course.Title,
                    Progress = e.ProgressPercentage,
                    e.EnrolledAt,
                    e.LastAccessed
                })
                .ToListAsync();
        }

        public async Task<IEnumerable<object>> GetTestResults(int teacherId)
        {
            return await TeacherTestResultsQuery(teacherId)
                .OrderByDescending(r => r.CompletedAt)
                .Select(r => new
                {
                    StudentId = r.StudentId,
                    StudentName = r.Student.FullName,
                    r.Student.Email,
                    TestId = r.TestId,
                    TestTitle = r.Test.Title,
                    LessonId = r.Test.LessonId,
                    LessonTitle = r.Test.Lesson.Title,
                    CourseTitle = r.Test.Lesson.Course.Title,
                    r.Score,
                    Status = r.Status.ToString(),
                    r.CompletedAt
                })
                .ToListAsync();
        }

        public async Task<object> GetReport(int teacherId)
        {
            var studentIds = ManagedStudentIdsQuery(teacherId);
            var courseIds = TeacherCourseIdsQuery(teacherId);
            var resultQuery = TeacherTestResultsQuery(teacherId);

            var courseReports = await _context.Courses
                .Where(c => c.CreatedBy == teacherId || c.TeacherId == teacherId)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    LessonCount = c.Lessons.Count,
                    StudentCount = c.Enrollments.Count(e => studentIds.Contains(e.StudentId)),
                    AvgProgress = Math.Round(c.Enrollments
                        .Where(e => studentIds.Contains(e.StudentId))
                        .Average(e => (double?)e.ProgressPercentage) ?? 0, 1)
                })
                .OrderByDescending(c => c.StudentCount)
                .ToListAsync();

            var passedCount = await resultQuery.CountAsync(r => r.Status == TestStatus.PASSED);
            var failedCount = await resultQuery.CountAsync(r => r.Status == TestStatus.FAILED);
            var inProgressCount = await resultQuery.CountAsync(r => r.Status == TestStatus.IN_PROGRESS);
            var avgProgress = await _context.Enrollments
                .Where(e => studentIds.Contains(e.StudentId) && courseIds.Contains(e.CourseId))
                .AverageAsync(e => (double?)e.ProgressPercentage) ?? 0;
            var avgScore = await resultQuery.AverageAsync(r => (double?)r.Score) ?? 0;

            return new
            {
                TotalStudents = await studentIds.CountAsync(),
                TotalCourses = await courseIds.CountAsync(),
                TotalLessons = await _context.Lessons.CountAsync(l => l.CreatedBy == teacherId),
                AverageProgress = Math.Round(avgProgress, 1),
                AverageScore = Math.Round(avgScore, 1),
                TestStatus = new
                {
                    Passed = passedCount,
                    Failed = failedCount,
                    InProgress = inProgressCount
                },
                Courses = courseReports
            };
        }

        public async Task<bool> EnrollStudentInCourseAsync(int teacherId, int courseId, string studentEmail)
        {
            var ownsCourse = await _context.Courses.AnyAsync(c => c.Id == courseId && (c.CreatedBy == teacherId || c.TeacherId == teacherId));
            if (!ownsCourse) return false;

            var email = studentEmail.Trim().ToLower();
            var student = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email && u.Role == UserRole.STUDENT);
            if (student == null) return false;

            // 1. Ensure Teacher-Student relationship exists
            var hasLink = await _context.TeacherStudents.AnyAsync(ts => ts.TeacherId == teacherId && ts.StudentId == student.Id);
            if (!hasLink)
            {
                _context.TeacherStudents.Add(new TeacherStudent { TeacherId = teacherId, StudentId = student.Id, CreatedAt = DateTime.UtcNow });
            }

            // 2. Ensure Enrollment exists
            var isEnrolled = await _context.Enrollments.AnyAsync(e => e.StudentId == student.Id && e.CourseId == courseId);
            if (!isEnrolled)
            {
                _context.Enrollments.Add(new Enrollment { StudentId = student.Id, CourseId = courseId, EnrolledAt = DateTime.UtcNow, ProgressPercentage = 0 });
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<object>> GetAllStudentsAsync()
        {
            return await _context.Users
                .Where(u => u.Role == UserRole.STUDENT)
                .Select(u => new
                {
                    u.Id,
                    u.FullName,
                    u.Email
                })
                .ToListAsync();
        }

        private IQueryable<int> ManagedStudentIdsQuery(int teacherId)
        {
            return _context.TeacherStudents
                .Where(ts => ts.TeacherId == teacherId)
                .Select(ts => ts.StudentId);
        }

        private IQueryable<int> TeacherCourseIdsQuery(int teacherId)
        {
            return _context.Courses
                .Where(c => c.CreatedBy == teacherId || c.TeacherId == teacherId)
                .Select(c => c.Id);
        }

        private IQueryable<TestResult> TeacherTestResultsQuery(int teacherId)
        {
            var studentIds = ManagedStudentIdsQuery(teacherId);

            return _context.TestResults
                .Include(r => r.Student)
                .Include(r => r.Test)
                    .ThenInclude(t => t.Lesson)
                    .ThenInclude(l => l.Course)
                .Where(r => studentIds.Contains(r.StudentId) && r.Test.Lesson.CreatedBy == teacherId);
        }

        private async Task<object?> GetLessonProjection(int teacherId, int lessonId)
        {
            var courseIds = TeacherCourseIdsQuery(teacherId);
            return await _context.Lessons
                .AsNoTracking()
                .Where(l => l.Id == lessonId && (l.CreatedBy == teacherId || courseIds.Contains(l.CourseId)))
                .Select(l => new
                {
                    l.Id,
                    l.CourseId,
                    CourseTitle = l.Course.Title,
                    l.Title,
                    l.Description,
                    l.VideoUrl,
                    PdfUrl = !string.IsNullOrWhiteSpace(l.PdfFileName) || !string.IsNullOrWhiteSpace(l.PdfContentType) ? $"/api/public/lessons/{l.Id}/pdf" : l.PdfUrl,
                    DocumentUrl = !string.IsNullOrWhiteSpace(l.DocumentFileName) || !string.IsNullOrWhiteSpace(l.DocumentContentType) ? $"/api/public/lessons/{l.Id}/document" : l.DocumentUrl,
                    DocumentName = l.DocumentFileName ?? l.DocumentName,
                    LessonPlanUrl = !string.IsNullOrWhiteSpace(l.LessonPlanFileName) || !string.IsNullOrWhiteSpace(l.LessonPlanContentType) ? $"/api/public/lessons/{l.Id}/lesson-plan" : null,
                    l.LessonPlanFileName,
                    SlideUrl = !string.IsNullOrWhiteSpace(l.SlideFileName) || !string.IsNullOrWhiteSpace(l.SlideContentType) ? $"/api/public/lessons/{l.Id}/slide" : null,
                    l.SlideFileName,
                    l.ArVrUrl,
                    l.QuizUrl,
                    QuizCount = string.IsNullOrWhiteSpace(l.QuizUrl) ? 0 : 1,
                    StudentCount = _context.Enrollments.Count(e => e.CourseId == l.CourseId),
                    Progress = Math.Round(_context.Enrollments
                        .Where(e => e.CourseId == l.CourseId)
                        .Average(e => (double?)e.ProgressPercentage) ?? 0, 1),
                    Date = l.CreatedAt.ToString("yyyy-MM-dd"),
                    l.CreatedAt,
                    l.UpdatedAt
                })
                .FirstOrDefaultAsync();
        }

        private static string GetContentType(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                return doc.RootElement.TryGetProperty("type", out var type) ? type.GetString() ?? "quiz" : "quiz";
            }
            catch
            {
                return "unknown";
            }
        }

        private static bool IsValidLearningContent(string content)
        {
            try
            {
                using var doc = JsonDocument.Parse(content);
                if (!doc.RootElement.TryGetProperty("type", out var type)) return false;
                var value = type.GetString();
                return value == "quiz" || value == "flashcard" || value == "pdf";
            }
            catch
            {
                return false;
            }
        }
    }
}
