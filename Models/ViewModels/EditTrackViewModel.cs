using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class EditTrackViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Введите название трека")]
        [StringLength(200)]
        [Display(Name = "Название трека")]
        public string Title { get; set; } = string.Empty;

        [StringLength(100)]
        [Display(Name = "Жанр")]
        public string? Genre { get; set; }

        [StringLength(50)]
        [Display(Name = "Тип вокала")]
        public string? VocalType { get; set; }

        [StringLength(50)]
        [Display(Name = "Настроение")]
        public string? Mood { get; set; }

        [DataType(DataType.MultilineText)]
        [Display(Name = "Текст песни (если есть)")]
        public string? Lyrics { get; set; }

        [Display(Name = "Аудиофайл (.mp3, .wav, .ogg)")]
        public IFormFile? AudioFile { get; set; } // 

        // --- Свойства для выпадающих списков ---
        public List<SelectListItem> AvailableGenres { get; set; } = new(); 
        public List<SelectListItem> AvailableVocalTypes { get; set; } = new();
        public List<SelectListItem> AvailableMoods { get; set; } = new(); 
    }
}