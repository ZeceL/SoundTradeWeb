using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering; // Для SelectListItem
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data;
using SoundTradeWebApp.Models;
using SoundTradeWebApp.Models.ViewModels;
using System.Security.Claims;
using System.IO;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using System.Collections.Generic; // Для List<>

namespace SoundTradeWebApp.Controllers
{
    [Authorize(Roles = "Author")]
    public class AuthorPanelController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuthorPanelController> _logger;

        public AuthorPanelController(ApplicationDbContext context, ILogger<AuthorPanelController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /AuthorPanel/Index - Отображает список треков автора
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                _logger.LogWarning("AuthorPanel/Index: Не удалось получить ID автора из клеймов.");
                return Unauthorized("Не удалось определить пользователя.");
            }

            // Выбираем данные для ViewModel
            var authorTrackViewModels = await _context.Tracks
                                           .AsNoTracking()
                                           .Where(t => t.AuthorUserId == userId)
                                           .OrderByDescending(t => t.UploadDate)
                                           .Select(t => new TrackIndexViewModel // Используем ViewModel
                                           {
                                               Id = t.Id,
                                               Title = t.Title,
                                               ArtistName = t.ArtistName, // Показываем имя, сохраненное в треке
                                               Genre = t.Genre,
                                               UploadDate = t.UploadDate
                                           })
                                           .ToListAsync();

            return View(authorTrackViewModels);
        }

        // GET: /AuthorPanel/Upload - Показывает форму загрузки
        public IActionResult Upload()
        {
            // Создаем ViewModel
            var viewModel = new UploadTrackViewModel();
            // Заполняем списки для dropdowns
            PopulateDropdownLists(viewModel);
            // Передаем ViewModel в представление
            return View(viewModel);
        }

        // POST: /AuthorPanel/Upload - Обрабатывает загрузку
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200 * 1024 * 1024)] // Пример лимита
        public async Task<IActionResult> Upload(UploadTrackViewModel model)
        {
            // Получаем ID и Имя пользователя
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var authorUsername = User.Identity?.Name; // Получаем Username из клейма

            if (!int.TryParse(userIdString, out int userId) || string.IsNullOrEmpty(authorUsername))
            {
                ModelState.AddModelError("", "Ошибка идентификации пользователя.");
                PopulateDropdownLists(model); // Перезаполняем списки перед возвратом!
                return View(model);
            }

            // Валидация файла...
            if (model.AudioFile == null || model.AudioFile.Length == 0)
            {
                ModelState.AddModelError(nameof(model.AudioFile), "Необходимо выбрать аудиофайл.");
            }
            // ... другие проверки файла ...

            if (ModelState.IsValid)
            {
                byte[] fileBytes;
                string contentType = model.AudioFile!.ContentType;
                string? fileNameForLog = model.AudioFile?.FileName;

                _logger.LogInformation("Attempting to upload track. Title: {TrackTitle}, FileName: {FileName}, Author: {AuthorUsername} ({AuthorId})",
                    model.Title, fileNameForLog, authorUsername, userId);

                try
                {
                    // Читаем файл в память
                    using (var memoryStream = new MemoryStream())
                    {
                        await model.AudioFile.CopyToAsync(memoryStream);
                        if (memoryStream.Length > 20 * 1024 * 1024) // Лимит 20MB
                        {
                            ModelState.AddModelError(nameof(model.AudioFile), "Файл превышает допустимый размер (20MB).");
                            PopulateDropdownLists(model); // Перезаполняем списки!
                            return View(model);
                        }
                        fileBytes = memoryStream.ToArray();
                    }
                    _logger.LogInformation("Файл '{FileName}' прочитан в память. Размер: {Length} байт.", fileNameForLog, fileBytes.Length);

                    // Создаем объект Track
                    var track = new Track
                    {
                        Title = model.Title,
                        ArtistName = authorUsername, // <-- Используем Username пользователя
                        Genre = model.Genre,
                        VocalType = model.VocalType,
                        Mood = model.Mood,
                        Lyrics = model.Lyrics,
                        AudioFileContent = fileBytes,
                        AudioContentType = contentType,
                        UploadDate = DateTime.UtcNow,
                        AuthorUserId = userId
                    };

                    // Сохраняем в базу данных
                    _context.Tracks.Add(track);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Трек '{TrackTitle}' (ID: {TrackId}) сохранен в БД автором {AuthorUsername} ({AuthorId}).",
                        track.Title, track.Id, authorUsername, userId);

                    TempData["SuccessMessage"] = $"Трек '{track.Title}' успешно загружен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "КРИТИЧЕСКАЯ ОШИБКА при загрузке трека '{TrackTitle}' автором {AuthorUsername} ({AuthorId}). FileName: {FileName}",
                         model.Title, authorUsername, userIdString, fileNameForLog); // Используем userIdString в логе ошибки
                    ModelState.AddModelError("", "Произошла внутренняя ошибка при сохранении трека.");
                }
            }
            else // Если ModelState НЕ IsValid
            {
                _logger.LogWarning("Model state is invalid for track upload. Title: {TrackTitle}, Author: {AuthorUsername} ({AuthorId})",
                    model.Title, authorUsername, userIdString);
                // Логируем ошибки валидации
                foreach (var state in ModelState)
                {
                    if (state.Value.Errors.Any())
                    {
                        _logger.LogWarning("Validation Error for {Field}: {Errors}", state.Key, string.Join(", ", state.Value.Errors.Select(e => e.ErrorMessage)));
                    }
                }
            }

            // Если модель невалидна или произошла ошибка,
            // перезаполняем списки и возвращаем форму
            PopulateDropdownLists(model);
            return View(model);
        }

        // --- Вспомогательный метод для заполнения списков ---
        private void PopulateDropdownLists(UploadTrackViewModel model)
        {
            // Инициализируем, только если еще не заполнены (на случай повторного вызова)
            if (!model.AvailableGenres.Any())
            {
                model.AvailableGenres = new List<SelectListItem>
                {
                    new() { Value = "", Text = "-- Выберите жанр --" }, // Пункт по умолчанию
                    new() { Value = "Pop", Text = "Поп" },
                    new() { Value = "Rock", Text = "Рок" },
                    new() { Value = "HipHop", Text = "Хип-хоп" },
                    new() { Value = "Electronic", Text = "Электроника" },
                    new() { Value = "Classical", Text = "Классика" },
                    new() { Value = "Jazz", Text = "Джаз" },
                    new() { Value = "Other", Text = "Другое" }
                    // Добавьте другие жанры по необходимости
                };
            }

            if (!model.AvailableVocalTypes.Any())
            {
                model.AvailableVocalTypes = new List<SelectListItem>
                {
                    new() { Value = "", Text = "-- Выберите тип вокала --" },
                    new() { Value = "Male", Text = "Мужской" },
                    new() { Value = "Female", Text = "Женский" },
                    new() { Value = "Mixed", Text = "Смешанный" }, // Добавлено
                    new() { Value = "Instrumental", Text = "Инструментал" }
                };
            }

            if (!model.AvailableMoods.Any())
            {
                model.AvailableMoods = new List<SelectListItem>
                {
                    new() { Value = "", Text = "-- Выберите настроение --" },
                    new() { Value = "Happy", Text = "Веселое" },
                    new() { Value = "Sad", Text = "Грустное" },
                    new() { Value = "Energetic", Text = "Энергичное" },
                    new() { Value = "Calm", Text = "Спокойное" },
                    new() { Value = "Romantic", Text = "Романтичное" }, // Добавлено
                    new() { Value = "Epic", Text = "Эпичное" },
                    new() { Value = "Other", Text = "Другое" }
                    // Добавьте другие настроения
                };
            }
        }
    }
}