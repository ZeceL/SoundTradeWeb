using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // Для Include и ToListAsync
using SoundTradeWebApp.Data;       // Контекст БД
using SoundTradeWebApp.Models;      // Модель Track

namespace SoundTradeWebApp.Controllers
{
    public class CatalogController : Controller
    {
        private readonly ILogger<CatalogController> _logger;
        private readonly ApplicationDbContext _context; // Добавляем DbContext

        // Обновляем конструктор
        public CatalogController(ILogger<CatalogController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context; // Внедряем зависимость
        }

        // Обновляем действие Index
        public async Task<IActionResult> Index()
        {
            // Загружаем все треки из базы данных
            // Используем Include, чтобы подтянуть связанное имя автора (если нужно)
            var tracks = await _context.Tracks
                                 // .Include(t => t.AuthorUser) // Раскомментируйте, если хотите отображать имя автора из связанной таблицы User
                                 .OrderByDescending(t => t.UploadDate) // Сортируем по дате
                                 .ToListAsync();

            // Передаем список треков в представление
            return View(tracks);
        }

        // Сюда можно добавить действия для фильтрации, поиска и т.д.
        // Например, для фильтрации по жанру:
        /*
        public async Task<IActionResult> Filter(string genre)
        {
            var filteredTracks = await _context.Tracks
                                        .Where(t => t.Genre == genre)
                                        .OrderByDescending(t => t.UploadDate)
                                        .ToListAsync();
            return View("Index", filteredTracks); // Используем то же представление Index
        }
        */
    }
}