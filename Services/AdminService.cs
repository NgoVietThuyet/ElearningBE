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

        public AdminService(AppDbContext context)
        {
            _context = context;
        }

        private static UserResponseDto ToUserResponse(User user)
        {
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
            // Check if email already exists
            if (await _context.Users.AnyAsync(x => x.Email == dto.Email))
            {
                throw new InvalidOperationException("Email already exists.");
            }

            var user = new User
            {
                FullName = dto.FullName,
                Email = dto.Email,
                PasswordHash = dto.Password, // Lưu plain text (dự án học tập)
                Role = dto.Role,
                DateOfBirth = dto.DateOfBirth,
                AvatarUrl = dto.AvatarUrl
            };

            await ApplyAvatarAsync(user, dto.AvatarFile);

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return ToUserResponse(user);
        }

        public async Task<UserResponseDto?> UpdateUserAsync(int id, UpdateUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return null;

            user.FullName = dto.FullName;
            user.Role = dto.Role;
            user.DateOfBirth = dto.DateOfBirth;
            user.AvatarUrl = dto.AvatarUrl;
            await ApplyAvatarAsync(user, dto.AvatarFile);
            user.UpdatedAt = DateTime.UtcNow;

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
                .Select(c => new CourseResponseDto
                {
                    Id = c.Id,
                    Title = c.Title,
                    Description = c.Description,
                    CreatedBy = c.CreatedBy,
                    CreatorName = c.Creator != null ? c.Creator.FullName : string.Empty,
                    TeacherId = c.TeacherId,
                    TeacherName = c.Teacher != null ? c.Teacher.FullName : string.Empty,
                    AvatarUrl = c.AvatarUrl,
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
                .FirstOrDefaultAsync(c => c.Id == id);

            if (course == null) return null;

            return new CourseResponseDto
            {
                Id = course.Id,
                Title = course.Title,
                Description = course.Description,
                CreatedBy = course.CreatedBy,
                CreatorName = course.Creator != null ? course.Creator.FullName : string.Empty,
                TeacherId = course.TeacherId,
                TeacherName = course.Teacher != null ? course.Teacher.FullName : string.Empty,
                AvatarUrl = course.AvatarUrl,
                CreatedAt = course.CreatedAt,
                UpdatedAt = course.UpdatedAt
            };
        }

        public async Task<CourseResponseDto> CreateCourseAsync(CourseDto courseDto, int adminId)
        {
            var course = new Course
            {
                Title = courseDto.Title,
                Description = courseDto.Description,
                AvatarUrl = courseDto.AvatarUrl,
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

            course.Title = courseDto.Title;
            course.Description = courseDto.Description;
            course.AvatarUrl = courseDto.AvatarUrl;
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
                    PdfUrl = l.PdfUrl,
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
                PdfUrl = lesson.PdfUrl,
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
                CreatedBy = adminId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

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
            lesson.UpdatedAt = DateTime.UtcNow;

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

        // --- Stats Methods ---
        public async Task<OverviewStatsDto> GetOverviewStatsAsync()
        {
            return new OverviewStatsDto
            {
                TotalUsers = await _context.Users.CountAsync(),
                TotalCourses = await _context.Courses.CountAsync(),
                TotalNews = await _context.News.CountAsync(),
                TotalLessons = await _context.Lessons.CountAsync()
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
