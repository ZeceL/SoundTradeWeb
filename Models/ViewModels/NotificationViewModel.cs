using SoundTradeWebApp.Enums;
using System;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class NotificationViewModel
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedDate { get; set; }
        public string? LinkUrl { get; set; } // Ссылка для перехода (например, на предложение обмена)
        public string TimeAgo { get; set; } = string.Empty; // Например, "5 минут назад"
    }
}