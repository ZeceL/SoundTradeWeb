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
        public string ArtistName { get; set; } = string.Empty;

        [Display(Name = "Жанр")]
        public string? Genre { get; set; }

        [Display(Name = "Дата загрузки")]
        [DisplayFormat(DataFormatString = "{0:dd.MM.yyyy HH:mm}")]
        public DateTime UploadDate { get; set; }
    }
}