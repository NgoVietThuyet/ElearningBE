using ElearningAPI.Dtos;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElearningAPI.Services
{
    public interface ITeacherService
    {
        Task<object> GetOverviewStats(int teacherId);
        Task<IEnumerable<object>> GetMyCourses(int teacherId);
        Task<IEnumerable<object>> GetMyStudents(int teacherId);
        Task<object?> GetStudentDetail(int teacherId, int studentId);
        Task<IEnumerable<object>> GetMyLessons(int teacherId);
        Task<IEnumerable<object>> GetMyFeedbacks(int teacherId);
        Task<bool> AddStudentToClass(int teacherId, string studentEmail);
        Task<object?> UpdateStudentAsync(int teacherId, int studentId, string fullName);
        Task<bool> RemoveStudentFromClass(int teacherId, int studentId);
        Task<object?> CreateLessonAsync(int teacherId, LessonDto dto);
        Task<object?> UpdateLessonAsync(int teacherId, int lessonId, LessonDto dto);
        Task<bool> DeleteLessonAsync(int teacherId, int lessonId);
        Task<IEnumerable<object>> GetLessonLearningItems(int teacherId, int lessonId);
        Task<object?> CreateLearningItem(int teacherId, int lessonId, string title, string content);
        Task<object?> UpdateLearningItem(int teacherId, int testId, string title, string content);
        Task<bool> DeleteLearningItem(int teacherId, int testId);
        Task<IEnumerable<object>> GetAcademicProgress(int teacherId);
        Task<IEnumerable<object>> GetTestResults(int teacherId);
        Task<object> GetReport(int teacherId);
        Task<bool> EnrollStudentInCourseAsync(int teacherId, int courseId, string studentEmail);
        Task<IEnumerable<object>> GetAllStudentsAsync();
    }
}
