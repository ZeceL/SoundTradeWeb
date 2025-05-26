using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data;
using SoundTradeWebApp.Models.ViewModels; // Для NotificationViewModel
using SoundTradeWebApp.Models;         // Для Notification
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SoundTradeWebApp.Controllers
{
    [Authorize] // Все действия требуют авторизации
    public class NotificationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NotificationController> _logger;

        public NotificationController(ApplicationDbContext context, ILogger<NotificationController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Notification/Index
        public async Task<IActionResult> Index()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var notifications = await _context.Notifications
                .Where(n => n.RecipientUserId == currentUserId)
                .OrderByDescending(n => n.CreatedDate)
                .Select(n => new NotificationViewModel
                {
                    Id = n.Id,
                    Message = n.Message,
                    Type = n.Type,
                    IsRead = n.IsRead,
                    CreatedDate = n.CreatedDate.ToLocalTime(), // Показываем локальное время
                    LinkUrl = n.LinkUrl,
                    TimeAgo = GetTimeAgo(n.CreatedDate.ToLocalTime()) // Для отображения "Х минут назад"
                })
                .ToListAsync();

            var unreadCount = notifications.Count(n => !n.IsRead);

            var viewModel = new NotificationIndexViewModel
            {
                Notifications = notifications,
                UnreadCount = unreadCount
            };

            return View(viewModel);
        }

        // POST: /Notification/MarkAsRead/5 (или AJAX)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.RecipientUserId == currentUserId);

            if (notification != null)
            {
                if (!notification.IsRead)
                {
                    notification.IsRead = true;
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Notification {NotificationId} marked as read for user {UserId}", id, currentUserId);
                }
                // Если уведомление имеет LinkUrl, можно перенаправить пользователя туда
                if (!string.IsNullOrEmpty(notification.LinkUrl))
                {
                    return Redirect(notification.LinkUrl);
                }
            }
            else
            {
                _logger.LogWarning("Attempt to mark non-existent or unauthorized notification {NotificationId} as read by user {UserId}", id, currentUserId);
                TempData["ErrorMessage"] = "Уведомление не найдено.";
            }
            return RedirectToAction(nameof(Index)); // Возвращаемся на страницу уведомлений
        }

        // POST: /Notification/MarkAllAsRead
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var unreadNotifications = await _context.Notifications
                .Where(n => n.RecipientUserId == currentUserId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }
                await _context.SaveChangesAsync();
                _logger.LogInformation("All unread notifications marked as read for user {UserId}", currentUserId);
                TempData["SuccessMessage"] = "Все уведомления отмечены как прочитанные.";
            }
            else
            {
                TempData["InfoMessage"] = "Нет непрочитанных уведомлений.";
            }
            return RedirectToAction(nameof(Index));
        }


        // GET: /Notification/GetUnreadCount (для AJAX)
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Json(new { count = 0 }); // или Unauthorized() для AJAX

            var count = await _context.Notifications
                .CountAsync(n => n.RecipientUserId == currentUserId && !n.IsRead);

            return Json(new { count });
        }


        // Вспомогательные методы
        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return 0;
        }

        private static string GetTimeAgo(DateTime dateTime)
        {
            var timeSpan = DateTime.Now - dateTime;

            if (timeSpan.TotalSeconds < 60) return $"{(int)timeSpan.TotalSeconds} сек назад";
            if (timeSpan.TotalMinutes < 60) return $"{(int)timeSpan.TotalMinutes} мин назад";
            if (timeSpan.TotalHours < 24) return $"{(int)timeSpan.TotalHours} ч назад";
            if (timeSpan.TotalDays < 7) return $"{(int)timeSpan.TotalDays} дн назад";
            if (timeSpan.TotalDays < 30) return $"{(int)(timeSpan.TotalDays / 7)} нед назад";
            if (timeSpan.TotalDays < 365) return $"{(int)(timeSpan.TotalDays / 30)} мес назад";
            return $"{(int)(timeSpan.TotalDays / 365)} г назад";
        }
    }
}
