// Путь: SoundTradeWebApp/Models/ViewModels/TrackIndexViewModel.cs
using System;
using System.ComponentModel.DataAnnotations;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class TrackIndexViewModel
    {
        public int Id { get; set; }
        [Display(Name = "Название")]
        public string Title { get; set; } = string.Empty;
        [Display(Name = "Исполнитель")]
        public string ArtistName { get; set; } = string.Empty; // Используем имя из трека
        [Display(Name = "Жанр")]
        public string? Genre { get; set; }
        [Display(Name = "Дата загрузки")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}")]
        public DateTime UploadDate { get; set; }
        // Добавим сюда поля, если они нужны в каталоге, но не было в панели автора
        public string? VocalType { get; set; }
        public string? Mood { get; set; }
        public string? Lyrics { get; set; } // Текст нужен для вкладки текстов
        public string AudioFilePath { get; set; } = string.Empty; // Путь к файлу или URL к действию GetAudio
    }
}