using Microsoft.AspNetCore.Mvc.Rendering; // Для SelectListItem
using System.Collections.Generic;       // Для List<>
using System;
using System.ComponentModel.DataAnnotations;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class CatalogViewModel
    {
        // Список треков для отображения (используем ViewModel из AuthorPanel)
        public List<TrackIndexViewModel> Tracks { get; set; } = new();

        // --- Опции для фильтров ---
        public List<SelectListItem> AvailableGenres { get; set; } = new();
        public List<SelectListItem> AvailableVocalTypes { get; set; } = new();
        public List<SelectListItem> AvailableMoods { get; set; } = new();

        // --- Выбранные значения фильтров ---
        // Имена должны совпадать с параметрами GET-запроса и именами <select>
        [Display(Name = "Жанр")]
        public string? SelectedGenre { get; set; }

        [Display(Name = "Тип вокала")]
        public string? SelectedVocalType { get; set; }

        [Display(Name = "Настроение")]
        public string? SelectedMood { get; set; }

        [Display(Name = "Поиск")] // Для <label asp-for="...">, если она нужна
        public string? SearchString { get; set; }
    }
}