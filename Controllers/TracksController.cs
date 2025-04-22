using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data;
using System.Threading.Tasks;

namespace SoundTradeWebApp.Controllers
{
    public class TracksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TracksController> _logger;

        public TracksController(ApplicationDbContext context, ILogger<TracksController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Tracks/GetAudio/{id}
        // Отдает аудиоданные для трека по ID
        [HttpGet]
        public async Task<IActionResult> GetAudio(int id)
        {
            // Выбираем только нужные поля для экономии памяти и трафика
            var trackData = await _context.Tracks
                                    .Where(t => t.Id == id)
                                    .Select(t => new { t.AudioFileContent, t.AudioContentType }) // Выбираем только байты и тип
                                    .FirstOrDefaultAsync();

            if (trackData == null || trackData.AudioFileContent == null || trackData.AudioFileContent.Length == 0)
            {
                _logger.LogWarning($"Аудиофайл для трека ID {id} не найден в БД.");
                return NotFound(); // 404 Not Found
            }

            _logger.LogInformation($"Отдача аудиофайла для трека ID {id}, тип: {trackData.AudioContentType}, размер: {trackData.AudioFileContent.Length} байт.");

            // Возвращаем FileResult из массива байт
            // enableRangeProcessing: true позволяет браузеру запрашивать части файла (для перемотки)
            return File(trackData.AudioFileContent, trackData.AudioContentType, enableRangeProcessing: true);
        }
    }
}