using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Для доступа к БД
using SoundTradeWebApp.Data;       // Контекст БД
using SoundTradeWebApp.Models;      // Для ErrorViewModel
using SoundTradeWebApp.Models.ViewModels; // Для TrackIndexViewModel
using System.Diagnostics;
using System.Linq;                 // Для .OrderByDescending, .Take, .Select
using System.Threading.Tasks;      // Для Task<>

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
            // Загружаем N последних треков для раздела "Рекомендуем"
            int numberOfRecommendedTracks = 6; // Сколько треков показывать

            var recommendedTracks = await _context.Tracks
                                        .AsNoTracking() // Только для чтения
                                        .OrderByDescending(t => t.UploadDate) // Сортируем по дате (сначала новые)
                                        .Take(numberOfRecommendedTracks)       // Берем первые N
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

            // Передаем список рекомендованных треков в представление
            return View(recommendedTracks);
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