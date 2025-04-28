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

        // GET: /AuthorPanel/Delete/{id} - Показывает страницу подтверждения удаления
        [HttpGet]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                _logger.LogWarning("AuthorPanel/Delete GET: Не удалось получить ID автора из клеймов.");
                return Unauthorized("Не удалось определить пользователя.");
            }

            var track = await _context.Tracks
                .FirstOrDefaultAsync(m => m.Id == id && m.AuthorUserId == userId); // Проверяем принадлежность трека текущему пользователю

            if (track == null)
            {
                return NotFound();
            }

            return View(track); // Вам потребуется создать представление Delete.cshtml для подтверждения
        }

        // POST: /AuthorPanel/Delete/{id} - Обрабатывает удаление трека
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                _logger.LogWarning("AuthorPanel/Delete POST: Не удалось получить ID автора из клеймов.");
                return Unauthorized("Не удалось определить пользователя.");
            }

            var track = await _context.Tracks
                .FirstOrDefaultAsync(m => m.Id == id && m.AuthorUserId == userId); // Снова проверяем принадлежность трека

            if (track == null)
            {
                // Трек уже был удален или никогда не существовал
                return RedirectToAction(nameof(Index));
            }

            try
            {
                _context.Tracks.Remove(track);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Трек '{TrackTitle}' (ID: {TrackId}) удален автором {AuthorId}.", track.Title, track.Id, userId);
                TempData["SuccessMessage"] = $"Трек '{track.Title}' успешно удален!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при удалении трека '{TrackTitle}' (ID: {TrackId}) автором {AuthorId}.", track.Title, track.Id, userId);
                TempData["ErrorMessage"] = $"Ошибка при удалении трека '{track.Title}'."; // Сообщение об ошибке для пользователя
                                                                                          // Возможно, стоит перенаправить на страницу ошибки или обратно на Index с сообщением
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: /AuthorPanel/Edit/{id} - Показывает форму редактирования трека
        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                _logger.LogWarning("AuthorPanel/Edit GET: Не удалось получить ID автора из клеймов.");
                return Unauthorized("Не удалось определить пользователя.");
            }

            // Находим трек и проверяем принадлежность текущему пользователю
            var track = await _context.Tracks
                .FirstOrDefaultAsync(t => t.Id == id && t.AuthorUserId == userId);

            if (track == null)
            {
                return NotFound();
            }

            // Создаем ViewModel и заполняем ее данными из трека
            var viewModel = new UploadTrackViewModel // Используем ту же ViewModel, что и для загрузки
            {
                Id = track.Id, // Добавляем ID для сохранения при редактировании
                Title = track.Title,
                // ArtistName не редактируется пользователем, он берется из клеймов
                Genre = track.Genre,
                VocalType = track.VocalType,
                Mood = track.Mood,
                Lyrics = track.Lyrics,
                // AudioFile сюда не загружается, это только для input type="file"
                // Добавьте поля, если у вас есть другие свойства трека, которые можно редактировать
            };

            // Заполняем списки для dropdowns
            PopulateDropdownLists(viewModel); // Используем тот же вспомогательный метод

            return View(viewModel);
        }

        // POST: /AuthorPanel/Edit/{id} - Обрабатывает отправку формы редактирования
        [HttpPost]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(200 * 1024 * 1024)] // Пример лимита, убедитесь, что соответствует вашим требованиям
        public async Task<IActionResult> Edit(UploadTrackViewModel model) // Принимаем ViewModel
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                ModelState.AddModelError("", "Ошибка идентификации пользователя.");
                PopulateDropdownLists(model);
                return View(model);
            }

            // Находим существующий трек по ID и проверяем принадлежность
            var existingTrack = await _context.Tracks
                .FirstOrDefaultAsync(t => t.Id == model.Id && t.AuthorUserId == userId);

            if (existingTrack == null)
            {
                return NotFound(); // Или другая обработка, если трек не найден (удален другим пользователем?)
            }

            // Валидация файла ТОЛЬКО если пользователь загрузил новый
            if (model.AudioFile != null && model.AudioFile.Length > 0)
            {
                // ... добавьте здесь те же проверки файла, что и в методе Upload ...
                if (model.AudioFile.Length > 20 * 1048576) // Пример лимита 20MB
                {
                    ModelState.AddModelError(nameof(model.AudioFile), "Новый файл превышает допустимый размер (20MB).");
                }
                // ... другие проверки типа файла и т.д. ...
            }


            if (ModelState.IsValid)
            {
                try
                {
                    // Обновляем свойства существующего трека из ViewModel
                    existingTrack.Title = model.Title;
                    existingTrack.Genre = model.Genre;
                    existingTrack.VocalType = model.VocalType;
                    existingTrack.Mood = model.Mood;
                    existingTrack.Lyrics = model.Lyrics;
                    // ArtistName не обновляется, так как зависит от пользователя

                    // Если загружен новый аудиофайл, обновляем его
                    if (model.AudioFile != null && model.AudioFile.Length > 0)
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            await model.AudioFile.CopyToAsync(memoryStream);
                            existingTrack.AudioFileContent = memoryStream.ToArray();
                            existingTrack.AudioContentType = model.AudioFile.ContentType;
                            // Возможно, стоит обновить дату загрузки, если файл новый?
                            // existingTrack.UploadDate = DateTime.UtcNow;
                        }
                    }
                    // Если файл не загружен, AudioFileContent и AudioContentType остаются прежними

                    _context.Update(existingTrack); // Помечаем трек как измененный
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Трек '{TrackTitle}' (ID: {TrackId}) обновлен автором {AuthorId}.", existingTrack.Title, existingTrack.Id, userId);
                    TempData["SuccessMessage"] = $"Трек '{existingTrack.Title}' успешно обновлен!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обновлении трека '{TrackTitle}' (ID: {TrackId}) автором {AuthorId}.", existingTrack.Title, existingTrack.Id, userId);
                    ModelState.AddModelError("", "Произошла внутренняя ошибка при сохранении изменений.");
                }
            }
            // Если модель невалидна или произошла ошибка, возвращаем форму с ошибками
            PopulateDropdownLists(model); // Перезаполняем списки перед возвратом
            return View(model);
        }
    }
}