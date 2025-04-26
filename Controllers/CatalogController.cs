using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data;
using SoundTradeWebApp.Models.ViewModels;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SoundTradeWebApp.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<CatalogController> _logger;

        public CatalogController(ApplicationDbContext context, ILogger<CatalogController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Catalog/Index
        // Добавляем параметр searchString
        public async Task<IActionResult> Index(string? searchString, string? selectedGenre, string? selectedVocalType, string? selectedMood)
        {
            _logger.LogInformation("Catalog Index requested. Search: '{SearchString}', Genre: {Genre}, Vocal: {Vocal}, Mood: {Mood}",
                searchString, selectedGenre, selectedVocalType, selectedMood);

            // 1. Начинаем строить запрос
            var tracksQuery = _context.Tracks.AsNoTracking();

            // --- 2. Применяем Фильтр Поиска ---
            if (!string.IsNullOrEmpty(searchString))
            {
                // Приводим поисковый запрос к нижнему регистру для регистронезависимого поиска
                string searchTerm = searchString.ToLower();
                // Ищем совпадения (содержит) в Названии или Имени Артиста
                tracksQuery = tracksQuery.Where(t =>
                    (t.Title != null && t.Title.ToLower().Contains(searchTerm)) ||
                    (t.ArtistName != null && t.ArtistName.ToLower().Contains(searchTerm))
                );
                _logger.LogInformation("Applying search filter: '{SearchTerm}'", searchTerm);
            }

            // --- 3. Применяем Фильтры Выпадающих Списков ---
            if (!string.IsNullOrEmpty(selectedGenre))
            {
                tracksQuery = tracksQuery.Where(t => t.Genre == selectedGenre);
            }
            if (!string.IsNullOrEmpty(selectedVocalType))
            {
                tracksQuery = tracksQuery.Where(t => t.VocalType == selectedVocalType);
            }
            if (!string.IsNullOrEmpty(selectedMood))
            {
                tracksQuery = tracksQuery.Where(t => t.Mood == selectedMood);
            }

            // 4. Выполняем запрос, сортируем и проецируем
            var filteredTracks = await tracksQuery
                                        .OrderByDescending(t => t.UploadDate)
                                        .Select(t => new TrackIndexViewModel
                                        {
                                            Id = t.Id,
                                            Title = t.Title,
                                            ArtistName = t.ArtistName,
                                            Genre = t.Genre,
                                            VocalType = t.VocalType,
                                            Mood = t.Mood,
                                            Lyrics = t.Lyrics,
                                            AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? "",
                                            UploadDate = t.UploadDate
                                        })
                                        .ToListAsync();
            _logger.LogInformation("Found {Count} tracks matching criteria.", filteredTracks.Count);

            // 5. Создаем и заполняем CatalogViewModel
            var viewModel = new CatalogViewModel
            {
                Tracks = filteredTracks,
                SelectedGenre = selectedGenre,
                SelectedVocalType = selectedVocalType,
                SelectedMood = selectedMood,
                SearchString = searchString // <-- Сохраняем поисковую строку для отображения в поле ввода
            };

            // 6. Заполняем списки для фильтров
            PopulateFilterDropdownLists(viewModel);

            // 7. Передаем ViewModel в представление
            return View(viewModel);
        }

        // --- Вспомогательный метод для заполнения списков фильтров ---
        private void PopulateFilterDropdownLists(CatalogViewModel model)
        {
            model.AvailableGenres = new List<SelectListItem> {
                new() { Value = "", Text = "Все жанры" }, new() { Value = "Pop", Text = "Поп" },
                new() { Value = "Rock", Text = "Рок" }, new() { Value = "HipHop", Text = "Хип-хоп" },
                new() { Value = "Electronic", Text = "Электроника" }, new() { Value = "Classical", Text = "Классика" },
                new() { Value = "Jazz", Text = "Джаз" }, new() { Value = "Other", Text = "Другое" } };

            model.AvailableVocalTypes = new List<SelectListItem> {
                new() { Value = "", Text = "Любой вокал" }, new() { Value = "Male", Text = "Мужской" },
                new() { Value = "Female", Text = "Женский" }, new() { Value = "Mixed", Text = "Смешанный" },
                new() { Value = "Instrumental", Text = "Инструментал" } };

            model.AvailableMoods = new List<SelectListItem> {
                new() { Value = "", Text = "Любое настроение" }, new() { Value = "Happy", Text = "Веселое" },
                new() { Value = "Sad", Text = "Грустное" }, new() { Value = "Energetic", Text = "Энергичное" },
                new() { Value = "Calm", Text = "Спокойное" }, new() { Value = "Romantic", Text = "Романтичное" },
                new() { Value = "Epic", Text = "Эпичное" }, new() { Value = "Other", Text = "Другое" } };
        }
    }
}