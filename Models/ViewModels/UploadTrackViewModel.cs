using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering; // Для SelectListItem
using System.Collections.Generic; // Для List<>

namespace SoundTradeWebApp.Models.ViewModels
{
    public class UploadTrackViewModel
    {
        [Required(ErrorMessage = "Введите название трека")]
        [StringLength(200)]
        [Display(Name = "Название трека")]
        public string Title { get; set; } = string.Empty;

        // Свойство ArtistName удалено - оно будет браться из пользователя

        [StringLength(100)]
        [Display(Name = "Жанр")]
        public string? Genre { get; set; } // Остается для получения выбранного значения

        [StringLength(50)]
        [Display(Name = "Тип вокала")]
        public string? VocalType { get; set; } // Остается для получения выбранного значения

        [StringLength(50)]
        [Display(Name = "Настроение")]
        public string? Mood { get; set; } // Остается для получения выбранного значения

        [DataType(DataType.MultilineText)]
        [Display(Name = "Текст песни (если есть)")]
        public string? Lyrics { get; set; }

        [Required(ErrorMessage = "Выберите аудиофайл для загрузки")]
        [Display(Name = "Аудиофайл (.mp3, .wav, .ogg)")]
        public IFormFile? AudioFile { get; set; }

        // --- Свойства для выпадающих списков ---
        // Инициализируем пустыми списками, чтобы избежать null reference
        public List<SelectListItem> AvailableGenres { get; set; } = new();
        public List<SelectListItem> AvailableVocalTypes { get; set; } = new();
        public List<SelectListItem> AvailableMoods { get; set; } = new();
    }
}