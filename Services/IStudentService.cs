using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElearningAPI.Services
{
    public interface IStudentService
    {
        Task<object> GetOverviewStats(int studentId);
        Task<IEnumerable<object>> GetAvailableCourses(int studentId);
        Task<IEnumerable<object>> GetMyCourses(int studentId);
        Task<IEnumerable<object>> GetMyLessons(int studentId);
        Task<object?> GetLessonDetail(int studentId, int lessonId, bool isAdmin = false);
        Task<object> EnrollCourse(int studentId, int courseId);
        Task<object> RequestEnrollCourseAsync(int studentId, int courseId);
        Task<object> GetEnrollmentStatusAsync(int studentId, int courseId);
        Task<object?> SubmitTest(int studentId, int testId, IEnumerable<int> answers);
        Task<IEnumerable<object>> GetTestHistory(int studentId);
        Task<bool> CompleteLesson(int studentId, int lessonId);
    }
}
