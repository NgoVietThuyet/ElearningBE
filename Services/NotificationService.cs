using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ElearningAPI.Data;
using ElearningAPI.Models;

namespace ElearningAPI.Services
{
    public class NotificationService : INotificationService
    {
        private readonly AppDbContext _context;
        private readonly ISseConnectionManager _sseManager;

        public NotificationService(AppDbContext context, ISseConnectionManager sseManager)
        {
            _context = context;
            _sseManager = sseManager;
        }

        public async Task<Notification> CreateNotificationAsync(int userId, string title, string message, string type, int? relatedId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title.Trim(),
                Message = message.Trim(),
                Type = type.Trim().ToUpperInvariant(),
                RelatedId = relatedId,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Push SSE notification in real-time
            try
            {
                var recipient = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
                if (recipient != null)
                {
                    var ssePayload = new
                    {
                        id = notification.Id,
                        userId = notification.UserId,
                        title = notification.Title,
                        message = notification.Message,
                        isRead = notification.IsRead,
                        createdAt = notification.CreatedAt.ToString("o"),
                        type = notification.Type,
                        relatedId = notification.RelatedId
                    };

                    if (recipient.Role == UserRole.ADMIN)
                    {
                        await _sseManager.BroadcastAsync("admin-stats", "notification-received", ssePayload);
                    }
                    else if (recipient.Role == UserRole.TEACHER)
                    {
                        await _sseManager.BroadcastAsync($"teacher-{userId}", "notification-received", ssePayload);
                    }
                    else if (recipient.Role == UserRole.STUDENT)
                    {
                        await _sseManager.BroadcastAsync($"student-{userId}", "notification-received", ssePayload);
                    }
                }
            }
            catch (Exception ex)
            {
                // Non-blocking log, fail silently to not block API transaction
                Console.WriteLine($"[SSE Notification Failed] {ex.Message}");
            }

            return notification;
        }

        public async Task CreateNotificationForAllAdminsAsync(string title, string message, string type, int? relatedId = null)
        {
            var admins = await _context.Users
                .AsNoTracking()
                .Where(u => u.Role == UserRole.ADMIN)
                .ToListAsync();

            foreach (var admin in admins)
            {
                await CreateNotificationAsync(admin.Id, title, message, type, relatedId);
            }
        }

        public async Task CreateNotificationForCourseStudentsAsync(int courseId, string title, string message, string type, int? relatedId = null)
        {
            var students = await _context.Enrollments
                .AsNoTracking()
                .Where(e => e.CourseId == courseId)
                .Select(e => e.StudentId)
                .ToListAsync();

            foreach (var studentId in students)
            {
                await CreateNotificationAsync(studentId, title, message, type, relatedId);
            }
        }

        public async Task<List<Notification>> GetUserNotificationsAsync(int userId, int limit = 50)
        {
            return await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Take(limit)
                .ToListAsync();
        }

        public async Task<bool> MarkAsReadAsync(int notificationId, int userId)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

            if (notification == null) return false;

            notification.IsRead = true;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MarkAllAsReadAsync(int userId)
        {
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Count == 0) return true;

            foreach (var n in unreadNotifications)
            {
                n.IsRead = true;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<int> GetUnreadCountAsync(int userId)
        {
            return await _context.Notifications
                .CountAsync(n => n.UserId == userId && !n.IsRead);
        }
    }
}
