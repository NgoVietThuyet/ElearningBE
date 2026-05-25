using System.Collections.Generic;
using System.Threading.Tasks;
using ElearningAPI.Models;

namespace ElearningAPI.Services
{
    public interface INotificationService
    {
        Task<Notification> CreateNotificationAsync(int userId, string title, string message, string type, int? relatedId = null);
        Task CreateNotificationForAllAdminsAsync(string title, string message, string type, int? relatedId = null);
        Task CreateNotificationForCourseStudentsAsync(int courseId, string title, string message, string type, int? relatedId = null);
        Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 50);
        Task<bool> MarkAsReadAsync(int notificationId, int userId);
        Task<bool> MarkAllAsReadAsync(int userId);
        Task<int> GetUnreadCountAsync(int userId);
    }
}
