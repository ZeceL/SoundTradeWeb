using System.Collections.Generic;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class NotificationIndexViewModel
    {
        public List<NotificationViewModel> Notifications { get; set; } = new List<NotificationViewModel>();
        public int UnreadCount { get; set; }
    }
}