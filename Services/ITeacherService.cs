using ElearningAPI.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElearningAPI.Services
{
    public interface ITeacherService
    {
        Task<object> GetOverviewStats(int teacherId);
        Task<IEnumerable<object>> GetMyStudents(int teacherId);
        Task<IEnumerable<object>> GetMyLessons(int teacherId);
        Task<IEnumerable<object>> GetMyFeedbacks(int teacherId);
        Task<bool> AddStudentToClass(int teacherId, string studentEmail);
    }
}
