using ElearningAPI.Data;
using ElearningAPI.Dtos;
using ElearningAPI.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace ElearningAPI.Services
{
    public class AdminService : IAdminService
    {
        private readonly AppDbContext _context;
        private static readonly HashSet<string> AllowedAvatarTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp"
        };
        private static readonly HashSet<string> AllowedPdfTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/pdf"
        };
        private static readonly HashSet<string> AllowedDocumentTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            "application/msword",
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
        };

        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
        {
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        private static DateTime? NormalizeUtcDate(DateTime? value)
        {
            if (!value.HasValue) return null;

            return value.Value.Kind switch
            {
                DateTimeKind.Utc => value.Value,
                DateTimeKind.Local => value.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            };
        }

        private static string? NormalizeNullableText(string? value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private async Task<int?> NormalizeTeacherIdAsync(int? teacherId)
        {
            if (!teacherId.HasValue || teacherId.Value <= 0) return null;

            var exists = await _context.Users.AnyAsync(u => u.Id == teacherId.Value && u.Role == UserRole.TEACHER);
            if (!exists)
                throw new InvalidOperationException("Giảng viên không tồn tại hoặc không đúng vai trò.");

            return teacherId.Value;
        }

        private async Task NormalizeCourseDtoAsync(CourseDto courseDto)
        {
            courseDto.Title = courseDto.Title?.Trim() ?? string.Empty;
            courseDto.Description = courseDto.Description?.Trim() ?? string.Empty;
            courseDto.AvatarUrl = string.IsNullOrWhiteSpace(courseDto.AvatarUrl) ? null : courseDto.AvatarUrl.Trim();
            courseDto.Code = courseDto.Code?.Trim().ToUpperInvariant() ?? string.Empty;
            courseDto.IntroVideoUrl = string.IsNullOrWhiteSpace(courseDto.IntroVideoUrl) ? null : courseDto.IntroVideoUrl.Trim();
            courseDto.Category = string.IsNullOrWhiteSpace(courseDto.Category) ? "Sinh học" : courseDto.Category.Trim();
            courseDto.Status = string.IsNullOrWhiteSpace(courseDto.Status) ? "Published" : courseDto.Status.Trim();
            courseDto.Level = courseDto.Level?.Trim() ?? string.Empty;
            courseDto.Language = string.IsNullOrWhiteSpace(courseDto.Language) ? "Tiếng Việt" : courseDto.Language.Trim();
            courseDto.DurationMinutes = Math.Max(0, courseDto.DurationMinutes);
            courseDto.ExpectedStudentCount = Math.Max(0, courseDto.ExpectedStudentCount);
            courseDto.StartDate = NormalizeUtcDate(courseDto.StartDate);
            courseDto.EndDate = NormalizeUtcDate(courseDto.EndDate);
            courseDto.LearningOutcomes = courseDto.LearningOutcomes?.Trim() ?? string.Empty;
            courseDto.TeacherId = await NormalizeTeacherIdAsync(courseDto.TeacherId);

            if (courseDto.EndDate.HasValue && courseDto.StartDate.HasValue && courseDto.EndDate < courseDto.StartDate)
                throw new InvalidOperationException("Ngày kết thúc phải lớn hơn hoặc bằng ngày bắt đầu.");
        }

        private async Task<int?> NormalizeAssignedCourseIdAsync(CreateUserDto dto)
        {
            dto.FullName = dto.FullName?.Trim() ?? string.Empty;
            dto.Email = dto.Email?.Trim() ?? string.Empty;
            dto.Gender = NormalizeNullableText(dto.Gender);
            dto.PhoneNumber = NormalizeNullableText(dto.PhoneNumber);
            dto.Address = NormalizeNullableText(dto.Address);
            dto.ShortBio = NormalizeNullableText(dto.ShortBio);
            dto.AvatarUrl = NormalizeNullableText(dto.AvatarUrl);
            dto.TeachingExperienceYears = Math.Max(0, dto.TeachingExperienceYears);

            if (dto.Role != UserRole.TEACHER)
            {
                dto.Gender = null;
                dto.PhoneNumber = null;
                dto.Address = null;
                dto.ShortBio = null;
                dto.TeachingExperienceYears = 0;
                dto.IsActive = true;
                return null;
            }

            if (!dto.AssignedCourseId.HasValue || dto.AssignedCourseId.Value <= 0) return null;

            var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.AssignedCourseId.Value);
            if (!courseExists)
                throw new InvalidOperationException("Khóa học phụ trách không tồn tại.");

            return dto.AssignedCourseId.Value;
        }

        private async Task<int?> NormalizeAssignedCourseIdAsync(UpdateUserDto dto)
        {
            dto.FullName = dto.FullName?.Trim() ?? string.Empty;
            dto.Gender = NormalizeNullableText(dto.Gender);
            dto.PhoneNumber = NormalizeNullableText(dto.PhoneNumber);
            dto.Address = NormalizeNullableText(dto.Address);
            dto.ShortBio = NormalizeNullableText(dto.ShortBio);
            dto.AvatarUrl = NormalizeNullableText(dto.AvatarUrl);
            dto.TeachingExperienceYears = Math.Max(0, dto.TeachingExperienceYears);

            if (dto.Role != UserRole.TEACHER)
            {
                dto.Gender = null;
                dto.PhoneNumber = null;
                dto.Address = null;
                dto.ShortBio = null;
                dto.TeachingExperienceYears = 0;
                dto.IsActive = true;
                return null;
            }

            if (!dto.AssignedCourseId.HasValue || dto.AssignedCourseId.Value <= 0) return null;

            var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.AssignedCourseId.Value);
            if (!courseExists)
                throw new InvalidOperationException("Khóa học phụ trách không tồn tại.");

            return dto.AssignedCourseId.Value;
        }

        private UserResponseDto ToUserResponse(User user)
        {
            var assignedCourse = _context.Courses
                .AsNoTracking()
                .Where(c => c.TeacherId == user.Id)
                .OrderByDescending(c => c.UpdatedAt)
                .Select(c => new { c.Id, c.Title })
                .FirstOrDefault();

            return new UserResponseDto
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email,
                Role = user.Role.ToString(),
                AvatarUrl = user.AvatarUrl,
                AvatarImageDataUrl = user.AvatarImage != null && !string.IsNullOrWhiteSpace(user.AvatarContentType)
                    ? $"data:{user.AvatarContentType};base64,{Convert.ToBase64String(user.AvatarImage)}"
                    : null,
                AvatarContentType = user.AvatarContentType,
                AvatarFileName = user.AvatarFileName,
                DateOfBirth = user.DateOfBirth,
                Gender = user.Gender,
                PhoneNumber = user.PhoneNumber,
                Address = user.Address,
                TeachingExperienceYears = user.TeachingExperienceYears,
                ShortBio = user.ShortBio,
                IsActive = user.IsActive,
                AssignedCourseId = assignedCourse?.Id,
                AssignedCourseTitle = assignedCourse?.Title,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt
            };
        }

        private static async Task ApplyAvatarAsync(User user, IFormFile? avatarFile)
        {
            if (avatarFile == null || avatarFile.Length == 0) return;

            if (!AllowedAvatarTypes.Contains(avatarFile.ContentType))
                throw new InvalidOperationException("Avatar must be a JPG, PNG, GIF, or WebP image.");

            const long maxAvatarBytes = 2 * 1024 * 1024;
            if (avatarFile.Length > maxAvatarBytes)
                throw new InvalidOperationException("Avatar image must be 2MB or smaller.");

            await using var stream = avatarFile.OpenReadStream();
            using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);

            user.AvatarImage = memory.ToArray();
            user.AvatarContentType = avatarFile.ContentType;
            user.AvatarFileName = Path.GetFileName(avatarFile.FileName);
            user.AvatarUrl = null;
        }

        private static async Task<byte[]> ReadFormFileAsync(IFormFile file)
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
                if (!AllowedDocumentTypes.Contains(dto.DocumentFile.ContentType))
                    throw new InvalidOperationException("Tệp Word không hợp lệ. Chỉ hỗ trợ DOC hoặc DOCX.");

                lesson.DocumentFile = await ReadFormFileAsync(dto.DocumentFile);
                lesson.DocumentContentType = dto.DocumentFile.ContentType;
                lesson.DocumentFileName = Path.GetFileName(dto.DocumentFile.FileName);
                lesson.DocumentName = lesson.DocumentFileName;
                lesson.DocumentUrl = null;
            }
        }

        private static string ResolveLessonPdfUrl(Lesson lesson)
        {
            if (lesson.PdfFile != null && lesson.PdfFile.Length > 0)
                return $"/api/public/lessons/{lesson.Id}/pdf";

            return lesson.PdfUrl;
        }

        private static string? ResolveLessonDocumentUrl(Lesson lesson)
        {
            if (lesson.DocumentFile != null && lesson.DocumentFile.Length > 0)
                return $"/api/public/lessons/{lesson.Id}/document";

            return lesson.DocumentUrl;
        }

        private static CourseMaterialResponseDto ToCourseMaterialResponse(CourseMaterial material)
        {
            return new CourseMaterialResponseDto
            {
                Id = material.Id,
                CourseId = material.CourseId,
                Title = material.Title,
                FileUrl = material.FileUrl,
                FileType = material.FileType,
                MimeType = material.MimeType,
                Description = material.Description,
                CreatedAt = material.CreatedAt,
                UpdatedAt = material.UpdatedAt
            };
        }

        private static LearningItemResponseDto ToLearningItemResponse(Test item, string type)
        {
            return new LearningItemResponseDto
            {
                Id = item.Id,
                CourseId = item.Lesson.CourseId,
                LessonId = item.LessonId,
                Title = item.Title,
                Type = type,
                Content = item.Content,
                CreatedAt = item.CreatedAt
            };
        }

        private static string GetLearningItemType(string content)
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(content);
                return doc.RootElement.TryGetProperty("type", out var type)
                    ? (type.GetString() ?? "quiz").Trim().ToLowerInvariant()
                    : "quiz";
            }
            catch
            {
                return "unknown";
            }
        }

        private static string NormalizeLearningItemType(string? type)
        {
            var normalized = (type ?? string.Empty).Trim().ToLowerInvariant();
            return normalized switch
            {
                "flashcard" => "flashcard",
                "quiz" => "quiz",
                "exam" => "exam",
                _ => throw new InvalidOperationException("Loại học liệu không hợp lệ.")
            };
        }

        private static string NormalizeJsonContent(string content, string type)
        {
            try
            {
                var node = System.Text.Json.Nodes.JsonNode.Parse(content)?.AsObject()
                    ?? throw new InvalidOperationException("Nội dung học liệu phải là object JSON hợp lệ.");

                node["type"] = type;
                return System.Text.Json.JsonSerializer.Serialize(node, JsonOptions);
            }
            catch
            {
                throw new InvalidOperationException("Nội dung học liệu phải là JSON hợp lệ.");
            }
        }

        // --- User Methods ---
        public async Task<IEnumerable<UserResponseDto>> GetAllUsersAsync()
        {
            var users = await _context.Users.AsNoTracking().ToListAsync();
            return users.Select(ToUserResponse);
        }

        public async Task<UserResponseDto?> GetUserByIdAsync(int id)
        {
            var u = await _context.Users.FindAsync(id);
            if (u == null) return null;

            return ToUserResponse(u);
        }

        public async Task<UserResponseDto> CreateUserAsync(CreateUserDto dto)
        {
            if (await _context.Users.AnyAsync(x => x.Email == dto.Email))
            {
                throw new InvalidOperationException("Email already exists.");
            }

            var assignedCourseId = await NormalizeAssignedCourseIdAsync(dto);

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = dto.Password,
                Role = dto.Role,
                DateOfBirth = dto.DateOfBirth,
                Gender = dto.Gender,
                PhoneNumber = dto.PhoneNumber,
                Address = dto.Address,
                TeachingExperienceYears = dto.TeachingExperienceYears,
                ShortBio = dto.ShortBio,
                IsActive = dto.IsActive,
                AvatarUrl = dto.AvatarUrl
            };

            await ApplyAvatarAsync(user, dto.AvatarFile);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            if (assignedCourseId.HasValue)
            {
                var course = await _context.Courses.FindAsync(assignedCourseId.Value);
                if (course != null)
                {
                    course.TeacherId = user.Id;
                    course.UpdatedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();
                }
            }

            return ToUserResponse(user);
        }

        public async Task<UserResponseDto?> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            var assignedCourseId = await NormalizeAssignedCourseIdAsync(dto);

            user.FullName = dto.FullName;
            user.Role = dto.Role;
            user.DateOfBirth = dto.DateOfBirth;
            user.Gender = dto.Gender;
            user.PhoneNumber = dto.PhoneNumber;
            user.Address = dto.Address;
            user.TeachingExperienceYears = dto.TeachingExperienceYears;
            user.ShortBio = dto.ShortBio;
            user.IsActive = dto.IsActive;
            user.AvatarUrl = dto.AvatarUrl;
            await ApplyAvatarAsync(user, dto.AvatarFile);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var teacherCourses = await _context.Courses.Where(c => c.TeacherId == user.Id).ToListAsync();
            foreach (var course in teacherCourses)
            {
                if (user.Role != UserRole.TEACHER || !assignedCourseId.HasValue || course.Id != assignedCourseId.Value)
                {
                    course.TeacherId = null;
                    course.UpdatedAt = DateTime.UtcNow;
                }
            }

            if (user.Role == UserRole.TEACHER && assignedCourseId.HasValue)
            {
                var assignedCourse = await _context.Courses.FindAsync(assignedCourseId.Value);
                if (assignedCourse != null)
                {
                    assignedCourse.TeacherId = user.Id;
                    assignedCourse.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return ToUserResponse(user);
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return false;

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- News Methods ---
        public async Task<IEnumerable<NewsResponseDto>> GetAllNewsAsync()
        {
            return await _context.News
                .Include(n => n.Author)
                .Select(n => new NewsResponseDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Content = n.Content,
                    AuthorId = n.AuthorId,
                    AuthorName = n.Author.FullName,
                    AvatarUrl = n.AvatarUrl,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<NewsResponseDto?> GetNewsByIdAsync(int id)
        {
            var news = await _context.News
                .Include(n => n.Author)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (news == null) return null;

            return new NewsResponseDto
            {
                Id = news.Id,
                Title = news.Title,
                Content = news.Content,
                AuthorId = news.AuthorId,
                AuthorName = news.Author.FullName,
                AvatarUrl = news.AvatarUrl,
                CreatedAt = news.CreatedAt,
                UpdatedAt = news.UpdatedAt
            };
        }

        public async Task<NewsResponseDto> CreateNewsAsync(NewsDto newsDto, int authorId)
        {
            var news = new News
            {
                Title = newsDto.Title,
                Content = newsDto.Content,
                AvatarUrl = newsDto.AvatarUrl,
                AuthorId = authorId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.News.Add(news);
            await _context.SaveChangesAsync();

            return await GetNewsByIdAsync(news.Id) ?? throw new Exception("Failed to create news");
        }

        public async Task<NewsResponseDto?> UpdateNewsAsync(int id, NewsDto newsDto)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return null;

            news.Title = newsDto.Title;
            news.Content = newsDto.Content;
            news.AvatarUrl = newsDto.AvatarUrl;
            news.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetNewsByIdAsync(news.Id);
        }

        public async Task<bool> DeleteNewsAsync(int id)
        {
            var news = await _context.News.FindAsync(id);
            if (news == null) return false;

            _context.News.Remove(news);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Course Methods ---
        public async Task<IEnumerable<CourseResponseDto>> GetAllCoursesAsync()
        {
            return await _context.Courses
                .Include(c => c.Creator)
                .Include(c => c.Teacher)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .Select(c => new CourseResponseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    Code = c.Code,
                    IntroVideoUrl = c.IntroVideoUrl,
                    Category = c.Category,
                    Status = c.Status,
                    Level = c.Level,
                    Language = c.Language,
                    DurationMinutes = c.DurationMinutes,
                    ExpectedStudentCount = c.ExpectedStudentCount,
                    StartDate = c.StartDate,
                    EndDate = c.EndDate,
                    LearningOutcomes = c.LearningOutcomes,
                    CreatedBy = c.CreatedBy,
                    CreatorName = c.Creator != null ? c.Creator.FullName : string.Empty,
                    TeacherId = c.TeacherId,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    TeacherAvatarUrl = c.Teacher != null ? c.Teacher.AvatarUrl : null,
                    AvatarUrl = c.AvatarUrl,
                    LessonCount = c.Lessons.Count,
                    StudentCount = c.ExpectedStudentCount > 0 ? c.ExpectedStudentCount : c.Enrollments.Count,
                    AverageProgress = c.Enrollments.Any() ? c.Enrollments.Average(e => e.ProgressPercentage) : 0,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<CourseResponseDto?> GetCourseByIdAsync(int id)
        {
            var course = await _context.Courses
                .Include(c => c.Creator)
                .Include(c => c.Teacher)
                .Include(c => c.Lessons)
                .Include(c => c.Enrollments)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return null;

            return new CourseResponseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                Code = course.Code,
                IntroVideoUrl = course.IntroVideoUrl,
                Category = course.Category,
                Status = course.Status,
                Level = course.Level,
                Language = course.Language,
                DurationMinutes = course.DurationMinutes,
                ExpectedStudentCount = course.ExpectedStudentCount,
                StartDate = course.StartDate,
                EndDate = course.EndDate,
                LearningOutcomes = course.LearningOutcomes,
                CreatedBy = course.CreatedBy,
                CreatorName = course.Creator != null ? course.Creator.FullName : string.Empty,
                TeacherId = course.TeacherId,
                TeacherName = course.Teacher != null ? course.Teacher.FullName : string.Empty,
                TeacherAvatarUrl = course.Teacher != null ? course.Teacher.AvatarUrl : null,
                AvatarUrl = course.AvatarUrl,
                LessonCount = course.Lessons.Count,
                StudentCount = course.ExpectedStudentCount > 0 ? course.ExpectedStudentCount : course.Enrollments.Count,
                AverageProgress = course.Enrollments.Any() ? course.Enrollments.Average(e => e.ProgressPercentage) : 0,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt
            };
        }

        public async Task<CourseResponseDto> CreateCourseAsync(CourseDto courseDto, int adminId)
        {
            await NormalizeCourseDtoAsync(courseDto);

            var course = new Course
            {
                Title = courseDto.Title,
                Description = courseDto.Description,
                AvatarUrl = courseDto.AvatarUrl,
                Code = courseDto.Code,
                IntroVideoUrl = courseDto.IntroVideoUrl,
                Category = courseDto.Category,
                Status = courseDto.Status,
                Level = courseDto.Level,
                Language = courseDto.Language,
                DurationMinutes = courseDto.DurationMinutes,
                ExpectedStudentCount = courseDto.ExpectedStudentCount,
                StartDate = courseDto.StartDate,
                EndDate = courseDto.EndDate,
                LearningOutcomes = courseDto.LearningOutcomes,
                CreatedBy = adminId,
                TeacherId = courseDto.TeacherId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Courses.Add(course);
            await _context.SaveChangesAsync();

            return await GetCourseByIdAsync(course.Id) ?? throw new Exception("Failed to create course");
        }

        public async Task<CourseResponseDto?> UpdateCourseAsync(int id, CourseDto courseDto)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return null;

            await NormalizeCourseDtoAsync(courseDto);

            course.Title = courseDto.Title;
            course.Description = courseDto.Description;
            course.AvatarUrl = courseDto.AvatarUrl;
            course.Code = courseDto.Code;
            course.IntroVideoUrl = courseDto.IntroVideoUrl;
            course.Category = courseDto.Category;
            course.Status = courseDto.Status;
            course.Level = courseDto.Level;
            course.Language = courseDto.Language;
            course.DurationMinutes = courseDto.DurationMinutes;
            course.ExpectedStudentCount = courseDto.ExpectedStudentCount;
            course.StartDate = courseDto.StartDate;
            course.EndDate = courseDto.EndDate;
            course.LearningOutcomes = courseDto.LearningOutcomes;
            course.TeacherId = courseDto.TeacherId;
            course.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return await GetCourseByIdAsync(course.Id);
        }

        public async Task<bool> DeleteCourseAsync(int id)
        {
            var course = await _context.Courses.FindAsync(id);
            if (course == null) return false;

            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Lesson Methods ---
        public async Task<IEnumerable<LessonResponseDto>> GetLessonsByCourseAsync(int courseId)
        {
            return await _context.Lessons
                .Include(l => l.Creator)
                .Where(l => l.CourseId == courseId)
                .Select(l => new LessonResponseDto
                {
                    Id = l.Id,
                    CourseId = l.CourseId,
                    Title = l.Title,
                    Description = l.Description,
                    VideoUrl = l.VideoUrl,
                    PdfUrl = l.PdfFile != null && l.PdfFile.Length > 0 ? $"/api/public/lessons/{l.Id}/pdf" : l.PdfUrl,
                    DocumentUrl = l.DocumentFile != null && l.DocumentFile.Length > 0 ? $"/api/public/lessons/{l.Id}/document" : l.DocumentUrl,
                    DocumentName = l.DocumentFileName ?? l.DocumentName,
                    CreatedBy = l.CreatedBy,
                    CreatorName = l.Creator != null ? l.Creator.FullName : string.Empty,
                    CreatedAt = l.CreatedAt,
                    UpdatedAt = l.UpdatedAt
                })
                .ToListAsync();
        }

        public async Task<LessonResponseDto?> GetLessonByIdAsync(int id)
        {
            var lesson = await _context.Lessons
                .Include(l => l.Creator)
                .FirstOrDefaultAsync(l => l.Id == id);

            if (lesson == null) return null;

            return new LessonResponseDto
            {
                Id = lesson.Id,
                CourseId = lesson.CourseId,
                Title = lesson.Title,
                Description = lesson.Description,
                VideoUrl = lesson.VideoUrl,
                PdfUrl = ResolveLessonPdfUrl(lesson),
                DocumentUrl = ResolveLessonDocumentUrl(lesson),
                DocumentName = lesson.DocumentFileName ?? lesson.DocumentName,
                CreatedBy = lesson.CreatedBy,
                CreatorName = lesson.Creator != null ? lesson.Creator.FullName : string.Empty,
                CreatedAt = lesson.CreatedAt,
                UpdatedAt = lesson.UpdatedAt
            };
        }

        public async Task<LessonResponseDto?> CreateLessonAsync(LessonDto lessonDto, int adminId)
        {
            var course = await _context.Courses.FindAsync(lessonDto.CourseId);
            if (course == null) return null;

            var lesson = new Lesson
            {
                CourseId = lessonDto.CourseId,
                Title = lessonDto.Title,
                Description = lessonDto.Description,
                VideoUrl = lessonDto.VideoUrl,
                PdfUrl = lessonDto.PdfUrl,
                DocumentUrl = lessonDto.DocumentUrl,
                DocumentName = lessonDto.DocumentName,
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await ApplyLessonFilesAsync(lesson, lessonDto);
            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();

            return await GetLessonByIdAsync(lesson.Id);
        }

        public async Task<LessonResponseDto?> UpdateLessonAsync(int id, LessonDto lessonDto)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return null;

            // Optional: verify if new courseId exists if it's changing
            if (lesson.CourseId != lessonDto.CourseId)
            {
                var course = await _context.Courses.FindAsync(lessonDto.CourseId);
                if (course == null) return null;
            }

            lesson.CourseId = lessonDto.CourseId;
            lesson.Title = lessonDto.Title;
            lesson.Description = lessonDto.Description;
            lesson.VideoUrl = lessonDto.VideoUrl;
            lesson.PdfUrl = lessonDto.PdfUrl;
            lesson.DocumentUrl = lessonDto.DocumentUrl;
            lesson.DocumentName = lessonDto.DocumentName;
            lesson.UpdatedAt = DateTime.UtcNow;

            await ApplyLessonFilesAsync(lesson, lessonDto);
            await _context.SaveChangesAsync();
            return await GetLessonByIdAsync(lesson.Id);
        }

        public async Task<bool> DeleteLessonAsync(int id)
        {
            var lesson = await _context.Lessons.FindAsync(id);
            if (lesson == null) return false;

            _context.Lessons.Remove(lesson);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<CourseMaterialResponseDto>> GetCourseMaterialsAsync(int courseId)
        {
            var materials = await _context.CourseMaterials
                .Where(m => m.CourseId == courseId)
                .OrderByDescending(m => m.UpdatedAt)
                .ToListAsync();

            return materials.Select(ToCourseMaterialResponse);
        }

        public async Task<CourseMaterialResponseDto?> CreateCourseMaterialAsync(CourseMaterialDto dto)
        {
            var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId);
            if (!courseExists) return null;

            var material = new CourseMaterial
            {
                CourseId = dto.CourseId,
                Title = dto.Title.Trim(),
                FileUrl = dto.FileUrl.Trim(),
                FileType = dto.FileType.Trim().ToLowerInvariant(),
                MimeType = dto.MimeType?.Trim() ?? string.Empty,
                Description = dto.Description?.Trim() ?? string.Empty,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.CourseMaterials.Add(material);
            await _context.SaveChangesAsync();

            return ToCourseMaterialResponse(material);
        }

        public async Task<CourseMaterialResponseDto?> UpdateCourseMaterialAsync(int id, CourseMaterialDto dto)
        {
            var material = await _context.CourseMaterials.FindAsync(id);
            if (material == null) return null;

            var courseExists = await _context.Courses.AnyAsync(c => c.Id == dto.CourseId);
            if (!courseExists) return null;

            material.CourseId = dto.CourseId;
            material.Title = dto.Title.Trim();
            material.FileUrl = dto.FileUrl.Trim();
            material.FileType = dto.FileType.Trim().ToLowerInvariant();
            material.MimeType = dto.MimeType?.Trim() ?? string.Empty;
            material.Description = dto.Description?.Trim() ?? string.Empty;
            material.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return ToCourseMaterialResponse(material);
        }

        public async Task<bool> DeleteCourseMaterialAsync(int id)
        {
            var material = await _context.CourseMaterials.FindAsync(id);
            if (material == null) return false;

            _context.CourseMaterials.Remove(material);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<LearningItemResponseDto>> GetCourseLearningItemsAsync(int courseId)
        {
            var items = await _context.Tests
                .Include(t => t.Lesson)
                .Where(t => t.Lesson.CourseId == courseId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            return items.Select(item => ToLearningItemResponse(item, GetLearningItemType(item.Content)));
        }

        public async Task<LearningItemResponseDto?> CreateLearningItemAsync(LearningItemDto dto)
        {
            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == dto.LessonId && l.CourseId == dto.CourseId);
            if (lesson == null) return null;

            var type = NormalizeLearningItemType(dto.Type);
            var item = new Test
            {
                LessonId = dto.LessonId,
                Title = dto.Title.Trim(),
                Content = NormalizeJsonContent(dto.Content, type),
                CreatedAt = DateTime.UtcNow
            };

            _context.Tests.Add(item);
            await _context.SaveChangesAsync();
            await _context.Entry(item).Reference(t => t.Lesson).LoadAsync();

            return ToLearningItemResponse(item, type);
        }

        public async Task<LearningItemResponseDto?> UpdateLearningItemAsync(int id, LearningItemDto dto)
        {
            var item = await _context.Tests
                .Include(t => t.Lesson)
                .FirstOrDefaultAsync(t => t.Id == id);
            if (item == null) return null;

            var lesson = await _context.Lessons.FirstOrDefaultAsync(l => l.Id == dto.LessonId && l.CourseId == dto.CourseId);
            if (lesson == null) return null;

            var type = NormalizeLearningItemType(dto.Type);
            item.LessonId = dto.LessonId;
            item.Title = dto.Title.Trim();
            item.Content = NormalizeJsonContent(dto.Content, type);
            await _context.SaveChangesAsync();
            await _context.Entry(item).Reference(t => t.Lesson).LoadAsync();

            return ToLearningItemResponse(item, type);
        }

        public async Task<bool> DeleteLearningItemAsync(int id)
        {
            var item = await _context.Tests.FindAsync(id);
            if (item == null) return false;

            _context.Tests.Remove(item);
            await _context.SaveChangesAsync();
            return true;
        }

        // --- Stats Methods ---
        public async Task<OverviewStatsDto> GetOverviewStatsAsync()
        {
            var courses = await _context.Courses.ToListAsync();
            var users = await _context.Users.ToListAsync();
            
            return new OverviewStatsDto
            {
                TotalUsers = users.Count,
                TotalCourses = courses.Count,
                TotalNews = await _context.News.CountAsync(),
                TotalLessons = await _context.Lessons.CountAsync(),
                UserStats = new UserManagementStatsDto
                {
                    Total = users.Count,
                    TotalTrend = 15,
                    Active = users.Count, // Mocking all active for now
                    ActiveTrend = 8,
                    Teacher = users.Count(u => u.Role == UserRole.TEACHER),
                    TeacherTrend = 2,
                    Student = users.Count(u => u.Role == UserRole.STUDENT),
                    StudentTrend = 12
                },
                CourseStats = new CourseManagementStatsDto
                {
                    Total = courses.Count,
                    TotalTrend = 12,
                    Published = courses.Count(c => c.Status == "Published"),
                    PublishedTrend = 8,
                    Draft = courses.Count(c => c.Status == "Draft"),
                    DraftTrend = -3,
                    Hidden = courses.Count(c => c.Status == "Hidden"),
                    HiddenTrend = 0
                }
            };
        }

        public async Task<IEnumerable<GpaDistributionDto>> GetGpaDistributionAsync()
        {
            var results = await _context.TestResults.ToListAsync();
            
            var excellent = results.Count(r => r.Score >= 8.5m);
            var good = results.Count(r => r.Score >= 7.0m && r.Score < 8.5m);
            var average = results.Count(r => r.Score >= 5.0m && r.Score < 7.0m);
            var poor = results.Count(r => r.Score < 5.0m);

            return new List<GpaDistributionDto>
            {
                new GpaDistributionDto { Range = "Giỏi (>= 8.5)", Count = excellent },
                new GpaDistributionDto { Range = "Khá (7.0 - 8.4)", Count = good },
                new GpaDistributionDto { Range = "Trung bình (5.0 - 6.9)", Count = average },
                new GpaDistributionDto { Range = "Yếu (< 5.0)", Count = poor }
            };
        }

        public async Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(int limit = 5)
        {
            var recentNews = await _context.News
                .Include(n => n.Author)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .Select(n => new RecentActivityDto
                {
                    Type = "Tin tức",
                    Title = n.Title,
                    Action = "Đăng tải bài viết mới",
                    By = n.Author.FullName,
                    Timestamp = n.CreatedAt
                })
                .ToListAsync();

            var recentCourses = await _context.Courses
                .Include(c => c.Creator)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .Select(c => new RecentActivityDto
                {
                    Type = "Khóa học",
                    Title = c.Title,
                    Action = "Tạo khóa học mới",
                    By = c.Creator.FullName,
                    Timestamp = c.CreatedAt
                })
                .ToListAsync();

            return recentNews.Concat(recentCourses)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .ToList();
        }
    }
}
