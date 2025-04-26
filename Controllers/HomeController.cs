using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Для доступа к БД
using SoundTradeWebApp.Data;       // Контекст БД
using SoundTradeWebApp.Models;      // Для ErrorViewModel
using SoundTradeWebApp.Models.ViewModels; // Для TrackIndexViewModel и IndexPageViewModel
using System.Diagnostics;
using System.Linq;                 // Для .OrderByDescending, .Take, .Select
using System.Threading.Tasks;      // Для Task<>
using System.Collections.Generic; // Для IEnumerable

namespace SoundTradeWebApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context; // <-- Добавляем контекст БД

        // Обновляем конструктор для внедрения DbContext
        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // <-- Внедряем зависимость
        }

        // Обновляем метод Index
        public async Task<IActionResult> Index()
        {
            int numberOfTracks = 6; // Сколько треков показывать в каждой секции

            var viewModel = new IndexPageViewModel();

            // Загружаем N последних треков для раздела "Рекомендуем"
            viewModel.RecommendedTracks = await _context.Tracks
                                        .AsNoTracking() // Только для чтения
                                        .OrderByDescending(t => t.UploadDate) // Сортируем по дате (сначала новые)
                                        .Take(numberOfTracks)       // Берем первые N
                                        .Select(t => new TrackIndexViewModel // Проецируем в ViewModel
                                        {
                                            Id = t.Id,
                                            Title = t.Title,
                                            ArtistName = t.ArtistName,
                                            Genre = t.Genre,
                                            // Генерируем URL для воспроизведения
                                            AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                            // Остальные поля (VocalType, Mood, Lyrics, UploadDate) можно добавить, если они нужны на главной
                                        })
                                        .ToListAsync();

            // Загружаем N последних треков для раздела "Поп музыка"
            viewModel.PopTracks = await _context.Tracks
                                    .AsNoTracking()
                                    .Where(t => t.Genre == "Pop") // Фильтруем по жанру "Поп"
                                    .OrderByDescending(t => t.UploadDate)
                                    .Take(numberOfTracks)
                                    .Select(t => new TrackIndexViewModel
                                    {
                                        Id = t.Id,
                                        Title = t.Title,
                                        ArtistName = t.ArtistName,
                                        Genre = t.Genre,
                                        AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                    })
                                    .ToListAsync();

            // Загружаем N последних треков для раздела "Рок музыка"
            viewModel.RockTracks = await _context.Tracks
                                    .AsNoTracking()
                                    .Where(t => t.Genre == "Rock") // Фильтруем по жанру "Рок"
                                    .OrderByDescending(t => t.UploadDate)
                                    .Take(numberOfTracks)
                                    .Select(t => new TrackIndexViewModel
                                    {
                                        Id = t.Id,
                                        Title = t.Title,
                                        ArtistName = t.ArtistName,
                                        Genre = t.Genre,
                                        AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                    })
                                    .ToListAsync();

            // Загружаем N последних треков для раздела "Хип-хоп музыка"
            viewModel.HipHopTracks = await _context.Tracks
                                        .AsNoTracking()
                                        .Where(t => t.Genre == "HipHop") // Фильтруем по жанру "Хип-хоп"
                                        .OrderByDescending(t => t.UploadDate)
                                        .Take(numberOfTracks)
                                        .Select(t => new TrackIndexViewModel
                                        {
                                            Id = t.Id,
                                            Title = t.Title,
                                            ArtistName = t.ArtistName,
                                            Genre = t.Genre,
                                            AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                        })
                                        .ToListAsync();

            // Загружаем N последних треков для раздела "Джаз музыка"
            viewModel.JazzTracks = await _context.Tracks
                                    .AsNoTracking()
                                    .Where(t => t.Genre == "Jazz") // Фильтруем по жанру "Джаз"
                                    .OrderByDescending(t => t.UploadDate)
                                    .Take(numberOfTracks)
                                    .Select(t => new TrackIndexViewModel
                                    {
                                        Id = t.Id,
                                        Title = t.Title,
                                        ArtistName = t.ArtistName,
                                        Genre = t.Genre,
                                        AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                    })
                                    .ToListAsync();

            // Загружаем N последних треков для раздела "Электронная музыка"
            viewModel.ElectronicTracks = await _context.Tracks
                                            .AsNoTracking()
                                            .Where(t => t.Genre == "Electronic") // Фильтруем по жанру "Электронная музыка"
                                            .OrderByDescending(t => t.UploadDate)
                                            .Take(numberOfTracks)
                                            .Select(t => new TrackIndexViewModel
                                            {
                                                Id = t.Id,
                                                Title = t.Title,
                                                ArtistName = t.ArtistName,
                                                Genre = t.Genre,
                                                AudioFilePath = Url.Action("GetAudio", "Tracks", new { id = t.Id }) ?? ""
                                            })
                                            .ToListAsync();


            // Передаем ViewModel с данными всех секций в представление
            return View(viewModel);
        }

        // Метод Error остается без изменений
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            _logger.LogError($"Error Occurred. TraceId: {errorViewModel.RequestId}");
            return View(errorViewModel);
        }
    }
}