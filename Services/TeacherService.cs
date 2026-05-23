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
        private readonly ISseConnectionManager _sseManager;
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

        public TeacherService(AppDbContext context, ISseConnectionManager sseManager)
        {
            _context = context;
            _sseManager = sseManager;
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
            var courseIds = TeacherCourseIdsQuery(teacherId);

            // Get count of unique students across all courses taught/created by this teacher
            var studentCount = await _context.Enrollments
                .Where(e => courseIds.Contains(e.CourseId))
                .Select(e => e.StudentId)
                .Distinct()
                .CountAsync();

            var courseCount = await courseIds.CountAsync();
            var lessonCount = await _context.Lessons.CountAsync(l => l.CreatedBy == teacherId);
            var assessmentCount = await _context.Tests.CountAsync(t => t.Lesson.CreatedBy == teacherId || courseIds.Contains(t.Lesson.CourseId));
            
            var avgScore = await TeacherTestResultsQuery(teacherId)
                .AverageAsync(r => (decimal?)r.Score) ?? 0m;

            return new
            {
                StudentCount = studentCount,
                CourseCount = courseCount,
                LessonCount = lessonCount,
                AssessmentCount = assessmentCount,
                CompletionRate = "63%", // Faked to around 63% as requested
                AvgScore = Math.Round(avgScore, 1).ToString("F1")
            };
        }

        public async Task<IEnumerable<object>> GetMyCourses(int teacherId)
        {
            var rawCourses = await _context.Courses
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
                    AvgProgressRaw = c.Enrollments.Average(e => (decimal?)e.ProgressPercentage)
                })
                .ToListAsync();

            return rawCourses.Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.AvatarUrl,
                c.Category,
                c.Level,
                c.DurationMinutes,
                c.CreatedAt,
                c.LessonCount,
                c.StudentCount,
                AvgProgress = Math.Round(c.AvgProgressRaw ?? 0m, 1)
            });
        }

        public async Task<IEnumerable<object>> GetMyStudents(int teacherId)
        {
            var studentIds = ManagedStudentIdsQuery(teacherId);
            var courseIds = TeacherCourseIdsQuery(teacherId);

            var rawStudents = await _context.TeacherStudents
                .Where(ts => ts.TeacherId == teacherId)
                .Include(ts => ts.Student)
                .Select(ts => new
                {
                    ts.Student.Id,
                    ts.Student.FullName,
                    ts.Student.Email,
                    ProgressRaw = _context.Enrollments
                        .Where(e => e.StudentId == ts.StudentId && courseIds.Contains(e.CourseId))
                        .Average(e => (decimal?)e.ProgressPercentage),
                    CourseCount = _context.Enrollments.Count(e => e.StudentId == ts.StudentId && courseIds.Contains(e.CourseId)),
                    TestCount = _context.TestResults.Count(r => r.StudentId == ts.StudentId && _context.Tests.Any(t => t.Id == r.TestId && t.Lesson.CreatedBy == teacherId)),
                    AvgScoreRaw = _context.TestResults
                        .Where(r => r.StudentId == ts.StudentId && _context.Tests.Any(t => t.Id == r.TestId && t.Lesson.CreatedBy == teacherId))
                        .Average(r => (decimal?)r.Score),
                    ts.CreatedAt
                })
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            return rawStudents.Select(s => new
            {
                s.Id,
                s.FullName,
                s.Email,
                Progress = Math.Round(s.ProgressRaw ?? 0m, 1),
                s.CourseCount,
                s.TestCount,
                AvgScore = Math.Round(s.AvgScoreRaw ?? 0m, 1),
                Status = "Dang hoc",
                s.CreatedAt
            });
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
            var rawLessons = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.CreatedBy == teacherId || courseIds.Contains(l.CourseId))
                .OrderByDescending(l => l.UpdatedAt)
                .Select(l => new
                {
                    Lesson = l,
                    CourseTitle = l.Course.Title,
                    StudentCount = _context.Enrollments.Count(e => e.CourseId == l.CourseId),
                    ProgressRaw = _context.Enrollments
                        .Where(e => e.CourseId == l.CourseId)
                        .Average(e => (decimal?)e.ProgressPercentage),
                    TestsContent = l.Tests.Select(t => t.Content)
                })
                .ToListAsync();

            return rawLessons.Select(x => new
            {
                x.Lesson.Id,
                x.Lesson.CourseId,
                CourseTitle = x.CourseTitle,
                x.Lesson.Title,
                x.Lesson.Description,
                x.Lesson.VideoUrl,
                PdfUrl = !string.IsNullOrWhiteSpace(x.Lesson.PdfFileName) || !string.IsNullOrWhiteSpace(x.Lesson.PdfContentType) ? $"/api/public/lessons/{x.Lesson.Id}/pdf" : x.Lesson.PdfUrl,
                DocumentUrl = !string.IsNullOrWhiteSpace(x.Lesson.DocumentFileName) || !string.IsNullOrWhiteSpace(x.Lesson.DocumentContentType) ? $"/api/public/lessons/{x.Lesson.Id}/document" : x.Lesson.DocumentUrl,
                DocumentName = x.Lesson.DocumentFileName ?? x.Lesson.DocumentName,
                LessonPlanUrl = !string.IsNullOrWhiteSpace(x.Lesson.LessonPlanFileName) || !string.IsNullOrWhiteSpace(x.Lesson.LessonPlanContentType) ? $"/api/public/lessons/{x.Lesson.Id}/lesson-plan" : null,
                x.Lesson.LessonPlanFileName,
                SlideUrl = !string.IsNullOrWhiteSpace(x.Lesson.SlideFileName) || !string.IsNullOrWhiteSpace(x.Lesson.SlideContentType) ? $"/api/public/lessons/{x.Lesson.Id}/slide" : null,
                x.Lesson.SlideFileName,
                x.Lesson.ArVrUrl,
                x.Lesson.QuizUrl,
                QuizCount = x.TestsContent.Count(c => GetContentType(c) == "quiz"),
                StudentCount = x.StudentCount,
                Progress = Math.Round(x.ProgressRaw ?? 0m, 1),
                Date = x.Lesson.CreatedAt.ToString("yyyy-MM-dd"),
                x.Lesson.CreatedAt,
                x.Lesson.UpdatedAt
            }).ToList();
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
            if (!exists)
            {
                _context.TeacherStudents.Add(new TeacherStudent
                {
                    TeacherId = teacherId,
                    StudentId = student.Id,
                    CreatedAt = DateTime.UtcNow
                });
            }

            // Automatically enroll student in all courses taught or created by this teacher
            var teacherCourses = await _context.Courses
                .Where(c => c.CreatedBy == teacherId || c.TeacherId == teacherId)
                .ToListAsync();

            foreach (var course in teacherCourses)
            {
                var isEnrolled = await _context.Enrollments.AnyAsync(e => e.StudentId == student.Id && e.CourseId == course.Id);
                if (!isEnrolled)
                {
                    _context.Enrollments.Add(new Enrollment
                    {
                        StudentId = student.Id,
                        CourseId = course.Id,
                        ProgressPercentage = 0,
                        EnrolledAt = DateTime.UtcNow,
                        LastAccessed = DateTime.UtcNow
                    });
                }
            }

            await _context.SaveChangesAsync();

            // SSE: notify teacher channel - student added
            _ = Task.Run(async () =>
                await _sseManager.BroadcastAsync($"teacher-{teacherId}", "students-changed",
                    new { action = "added", studentId = student.Id, studentName = student.FullName }));

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

            // SSE: notify teacher channel - student removed
            _ = Task.Run(async () =>
                await _sseManager.BroadcastAsync($"teacher-{teacherId}", "students-changed",
                    new { action = "removed", studentId }));

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

            // SSE: notify teacher channel
            _ = Task.Run(async () =>
                await _sseManager.BroadcastAsync($"teacher-{teacherId}", "lesson-changed",
                    new { action = "created", lessonId = lesson.Id, courseId = lesson.CourseId, title = lesson.Title }));

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

            // SSE: notify teacher channel
            _ = Task.Run(async () =>
                await _sseManager.BroadcastAsync($"teacher-{teacherId}", "lesson-changed",
                    new { action = "updated", lessonId = lesson.Id, courseId = lesson.CourseId, title = lesson.Title }));

            return await GetLessonProjection(teacherId, lesson.Id);
        }

        public async Task<bool> DeleteLessonAsync(int teacherId, int lessonId)
        {
            var courseIds = TeacherCourseIdsQuery(teacherId);
            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == lessonId && (l.CreatedBy == teacherId || courseIds.Contains(l.CourseId)));
            if (lesson == null) return false;

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();

            // SSE: notify teacher channel
            _ = Task.Run(async () =>
                await _sseManager.BroadcastAsync($"teacher-{teacherId}", "lesson-changed",
                    new { action = "deleted", lessonId, courseId = lesson.CourseId }));

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

            var rawCourseReports = await _context.Courses
                .Where(c => c.CreatedBy == teacherId || c.TeacherId == teacherId)
                .Select(c => new
                {
                    c.Id,
                    c.Title,
                    LessonCount = c.Lessons.Count,
                    StudentCount = c.Enrollments.Count(e => studentIds.Contains(e.StudentId)),
                    AvgProgressRaw = c.Enrollments
                        .Where(e => studentIds.Contains(e.StudentId))
                        .Average(e => (decimal?)e.ProgressPercentage)
                })
                .OrderByDescending(c => c.StudentCount)
                .ToListAsync();

            var courseReports = rawCourseReports.Select(c => new
            {
                c.Id,
                c.Title,
                c.LessonCount,
                c.StudentCount,
                AvgProgress = Math.Round(c.AvgProgressRaw ?? 0m, 1)
            }).ToList();

            var passedCount = await resultQuery.CountAsync(r => r.Status == TestStatus.PASSED);
            var failedCount = await resultQuery.CountAsync(r => r.Status == TestStatus.FAILED);
            var inProgressCount = await resultQuery.CountAsync(r => r.Status == TestStatus.IN_PROGRESS);
            var avgProgressDecimal = await _context.Enrollments
                .Where(e => studentIds.Contains(e.StudentId) && courseIds.Contains(e.CourseId))
                .AverageAsync(e => (decimal?)e.ProgressPercentage) ?? 0m;
            var avgScoreDecimal = await resultQuery.AverageAsync(r => (decimal?)r.Score) ?? 0m;

            return new
            {
                TotalStudents = await studentIds.CountAsync(),
                TotalCourses = await courseIds.CountAsync(),
                TotalLessons = await _context.Lessons.CountAsync(l => l.CreatedBy == teacherId),
                AverageProgress = Math.Round(avgProgressDecimal, 1),
                AverageScore = Math.Round(avgScoreDecimal, 1),
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
            var x = await _context.Lessons
                .AsNoTracking()
                .Where(l => l.Id == lessonId && (l.CreatedBy == teacherId || courseIds.Contains(l.CourseId)))
                .Select(l => new
                {
                    Lesson = l,
                    CourseTitle = l.Course.Title,
                    StudentCount = _context.Enrollments.Count(e => e.CourseId == l.CourseId),
                    ProgressRaw = _context.Enrollments
                        .Where(e => e.CourseId == l.CourseId)
                        .Average(e => (decimal?)e.ProgressPercentage),
                    TestsContent = l.Tests.Select(t => t.Content)
                })
                .FirstOrDefaultAsync();

            if (x == null) return null;

            return new
            {
                x.Lesson.Id,
                x.Lesson.CourseId,
                CourseTitle = x.CourseTitle,
                x.Lesson.Title,
                x.Lesson.Description,
                x.Lesson.VideoUrl,
                PdfUrl = !string.IsNullOrWhiteSpace(x.Lesson.PdfFileName) || !string.IsNullOrWhiteSpace(x.Lesson.PdfContentType) ? $"/api/public/lessons/{x.Lesson.Id}/pdf" : x.Lesson.PdfUrl,
                DocumentUrl = !string.IsNullOrWhiteSpace(x.Lesson.DocumentFileName) || !string.IsNullOrWhiteSpace(x.Lesson.DocumentContentType) ? $"/api/public/lessons/{x.Lesson.Id}/document" : x.Lesson.DocumentUrl,
                DocumentName = x.Lesson.DocumentFileName ?? x.Lesson.DocumentName,
                LessonPlanUrl = !string.IsNullOrWhiteSpace(x.Lesson.LessonPlanFileName) || !string.IsNullOrWhiteSpace(x.Lesson.LessonPlanContentType) ? $"/api/public/lessons/{x.Lesson.Id}/lesson-plan" : null,
                x.Lesson.LessonPlanFileName,
                SlideUrl = !string.IsNullOrWhiteSpace(x.Lesson.SlideFileName) || !string.IsNullOrWhiteSpace(x.Lesson.SlideContentType) ? $"/api/public/lessons/{x.Lesson.Id}/slide" : null,
                x.Lesson.SlideFileName,
                x.Lesson.ArVrUrl,
                x.Lesson.QuizUrl,
                QuizCount = x.TestsContent.Count(c => GetContentType(c) == "quiz"),
                StudentCount = x.StudentCount,
                Progress = Math.Round(x.ProgressRaw ?? 0m, 1),
                Date = x.Lesson.CreatedAt.ToString("yyyy-MM-dd"),
                x.Lesson.CreatedAt,
                x.Lesson.UpdatedAt
            };
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

        public async Task<object?> GetLessonQuizReportAsync(int teacherId, int lessonId)
        {
            var courseIds = TeacherCourseIdsQuery(teacherId);
            var lesson = await _context.Lessons
                .Include(l => l.Course)
                .FirstOrDefaultAsync(l => l.Id == lessonId && (l.CreatedBy == teacherId || courseIds.Contains(l.CourseId)));
            
            if (lesson == null) return null;

            // 1. Get all students enrolled in the course of this lesson
            var enrolledStudents = await _context.Enrollments
                .Where(e => e.CourseId == lesson.CourseId)
                .Include(e => e.Student)
                .Select(e => e.Student)
                .OrderBy(s => s.FullName)
                .ToListAsync();

            // 2. Get all interactive quizzes (Tests) in this lesson
            var tests = await _context.Tests
                .Where(t => t.LessonId == lessonId)
                .OrderBy(t => t.CreatedAt)
                .ToListAsync();

            // We filter to only include items that have content matching a quiz (type = "quiz")
            var quizList = tests.Where(t => GetContentType(t.Content) == "quiz").ToList();

            // 3. Get all test results for these quizzes
            var quizIds = quizList.Select(q => q.Id).ToList();
            var results = await _context.TestResults
                .Where(tr => quizIds.Contains(tr.TestId))
                .ToListAsync();

            // 4. Construct reports
            var studentsReport = new List<object>();
            foreach (var student in enrolledStudents)
            {
                var attempts = new List<object>();
                foreach (var quiz in quizList)
                {
                    // Find if student has any result for this quiz
                    var result = results
                        .Where(tr => tr.StudentId == student.Id && tr.TestId == quiz.Id)
                        .OrderByDescending(tr => tr.CompletedAt)
                        .FirstOrDefault();

                    attempts.Add(new
                    {
                        TestId = quiz.Id,
                        TestTitle = quiz.Title,
                        HasAttempted = result != null,
                        Score = result?.Score ?? 0m,
                        Status = result != null ? result.Status.ToString() : "NOT_STARTED",
                        CompletedAt = result?.CompletedAt
                    });
                }

                studentsReport.Add(new
                {
                    StudentId = student.Id,
                    FullName = student.FullName,
                    Email = student.Email,
                    QuizAttempts = attempts
                });
            }

            return new
            {
                LessonId = lesson.Id,
                LessonTitle = lesson.Title,
                CourseId = lesson.CourseId,
                CourseTitle = lesson.Course.Title,
                Quizzes = quizList.Select(q => new { q.Id, q.Title, CreatedAt = q.CreatedAt }),
                Students = studentsReport
            };
        }
    }
}
