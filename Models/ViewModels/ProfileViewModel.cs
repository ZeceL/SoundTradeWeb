using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class ProfileViewModel
    {
        [Display(Name = "Логин")]
        public string Username { get; set; } = string.Empty;

        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        [Display(Name = "Тип пользователя")]
        public string UserType { get; set; } = string.Empty;

        // --- ИЗМЕНЕНО: Храним ID и Title ---
        // Простая вложенная запись для хранения информации о треке
        public record TrackInfo(int Id, string Title);
        // Список треков для отображения
        public List<TrackInfo> UploadedTracks { get; set; } = new List<TrackInfo>();
        // --- Конец изменения ---
    }
}