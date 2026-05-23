using ElearningAPI.Dtos;

namespace ElearningAPI.Services
{
    public interface IAdminService
    {
        Task<IEnumerable<UserResponseDto>> GetAllUsersAsync();
        Task<UserResponseDto?> GetUserByIdAsync(int id);
        Task<UserResponseDto> CreateUserAsync(CreateUserDto dto);
        Task<UserResponseDto?> UpdateUserAsync(int id, UpdateUserDto dto);
        Task<bool> DeleteUserAsync(int id);

        // News CRUD
        Task<IEnumerable<NewsResponseDto>> GetAllNewsAsync();
        Task<NewsResponseDto?> GetNewsByIdAsync(int id);
        Task<NewsResponseDto> CreateNewsAsync(NewsDto newsDto, int authorId);
        Task<NewsResponseDto?> UpdateNewsAsync(int id, NewsDto newsDto);
        Task<bool> DeleteNewsAsync(int id);

        // Course CRUD
        Task<IEnumerable<CourseResponseDto>> GetAllCoursesAsync();
        Task<CourseResponseDto?> GetCourseByIdAsync(int id);
        Task<CourseResponseDto> CreateCourseAsync(CourseDto courseDto, int adminId);
        Task<CourseResponseDto?> UpdateCourseAsync(int id, CourseDto courseDto);
        Task<bool> DeleteCourseAsync(int id);
        Task<bool> UpdateCourseOrderAsync(List<int> courseIds);

        // Lesson CRUD
        Task<IEnumerable<LessonResponseDto>> GetLessonsByCourseAsync(int courseId);
        Task<LessonResponseDto?> GetLessonByIdAsync(int id);
        Task<LessonResponseDto?> CreateLessonAsync(LessonDto lessonDto, int adminId);
        Task<LessonResponseDto?> UpdateLessonAsync(int id, LessonDto lessonDto);
        Task<bool> DeleteLessonAsync(int id);

        Task<IEnumerable<CourseMaterialResponseDto>> GetCourseMaterialsAsync(int courseId);
        Task<CourseMaterialResponseDto?> CreateCourseMaterialAsync(CourseMaterialDto dto);
        Task<CourseMaterialResponseDto?> UpdateCourseMaterialAsync(int id, CourseMaterialDto dto);
        Task<bool> DeleteCourseMaterialAsync(int id);

        Task<IEnumerable<LearningItemResponseDto>> GetCourseLearningItemsAsync(int courseId);
        Task<LearningItemResponseDto?> CreateLearningItemAsync(LearningItemDto dto);
        Task<LearningItemResponseDto?> UpdateLearningItemAsync(int id, LearningItemDto dto);
        Task<bool> DeleteLearningItemAsync(int id);

        // Stats
        Task<OverviewStatsDto> GetOverviewStatsAsync();
        Task<IEnumerable<GpaDistributionDto>> GetGpaDistributionAsync();
        Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(int limit = 5);
        Task<IEnumerable<CourseCompletionDto>> GetCourseCompletionAsync();
        Task<IEnumerable<MemberGrowthDto>> GetMemberGrowthAsync();
    }
}
