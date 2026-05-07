using System.Collections.Generic;
using System.Threading.Tasks;

namespace ElearningAPI.Services
{
    public interface IStudentService
    {
        Task<object> GetOverviewStats(int studentId);
        Task<IEnumerable<object>> GetMyCourses(int studentId);
        Task<IEnumerable<object>> GetMyLessons(int studentId);
    }
}
