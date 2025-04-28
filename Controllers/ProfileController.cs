using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data;
using SoundTradeWebApp.Models;
using SoundTradeWebApp.Models.ViewModels;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Collections.Generic; // Для List<>

namespace SoundTradeWebApp.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(ApplicationDbContext context, ILogger<ProfileController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Profile/Index
        public async Task<IActionResult> Index()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                _logger.LogWarning("Profile/Index: Не удалось получить ID пользователя.");
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users
                                 .Include(u => u.Tracks) // Включаем треки для получения списка
                                 .AsNoTracking()
                                 .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                _logger.LogWarning("Profile/Index: Пользователь с ID {UserId} не найден в БД.", userId);
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new ProfileViewModel
            {
                Username = user.Username,
                Email = user.Email,
                UserType = user.UserType,
                // --- ИЗМЕНЕНО: Заполняем новый список ---
                UploadedTracks = user.Tracks
                                     .OrderBy(t => t.Title)
                                     // Создаем объекты TrackInfo с ID и Title
                                     .Select(t => new ProfileViewModel.TrackInfo(t.Id, t.Title))
                                     .ToList()
                // --- Конец изменения ---
            };

            return View(viewModel);
        }

        // GET: /Profile/Edit
        public async Task<IActionResult> Edit()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId)) { return RedirectToAction("Login", "Account"); }

            var user = await _context.Users
                                 .AsNoTracking()
                                 .Select(u => new { u.Id, u.Username, u.Email })
                                 .FirstOrDefaultAsync(u => u.Id == userId);

            if (user == null)
            {
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new EditProfileViewModel
            {
                Username = user.Username,
                Email = user.Email
            };
            return View(viewModel);
        }

        // POST: /Profile/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(EditProfileViewModel model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                _logger.LogError("Profile/Edit POST: Не удалось получить UserID из клеймов.");
                TempData["ErrorMessage"] = "Ошибка сессии. Пожалуйста, войдите снова.";
                return RedirectToAction("Login", "Account");
            }

            if (!string.IsNullOrEmpty(model.NewPassword) && model.NewPassword != model.ConfirmPassword)
            {
                ModelState.AddModelError(nameof(model.ConfirmPassword), "Пароли не совпадают.");
            }

            if (ModelState.IsValid)
            {
                // Находим пользователя в БД для ИЗМЕНЕНИЯ (отслеживаем)
                var userToUpdate = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);

                if (userToUpdate == null) { /* ... обработка ошибки ... */ }

                bool changesMade = false;
                bool needsReLogin = false;
                string oldUsername = userToUpdate.Username; // Сохраним старое имя для лога

                // --- Проверка и обновление Username ---
                if (userToUpdate.Username != model.Username)
                {
                    bool usernameExists = await _context.Users.AnyAsync(u => u.Id != userId && u.Username == model.Username);
                    if (usernameExists)
                    {
                        ModelState.AddModelError(nameof(model.Username), "Этот логин уже занят.");
                    }
                    else
                    {
                        _logger.LogInformation("Обновление Username для User ID {UserId} с '{OldValue}' на '{NewValue}'", userId, userToUpdate.Username, model.Username);
                        userToUpdate.Username = model.Username;
                        changesMade = true;
                        needsReLogin = true;

                        // --- ↓↓↓ НОВОЕ: Обновление ArtistName в треках ↓↓↓ ---
                        try
                        {
                            _logger.LogInformation("Обновление ArtistName в треках для User ID {UserId}", userId);
                            // Загружаем треки пользователя для обновления
                            var tracksToUpdate = await _context.Tracks
                                                         .Where(t => t.AuthorUserId == userId)
                                                         .ToListAsync(); // Загружаем, чтобы EF их отслеживал

                            int updatedCount = 0;
                            foreach (var track in tracksToUpdate)
                            {
                                // Обновляем имя только если оно отличалось от нового (на всякий случай)
                                if (track.ArtistName != model.Username)
                                {
                                    track.ArtistName = model.Username;
                                    updatedCount++;
                                }
                            }
                            _logger.LogInformation("Обновлено ArtistName для {Count} треков User ID {UserId}.", updatedCount, userId);
                            // SaveChangesAsync ниже сохранит изменения и в пользователе, и в треках
                        }
                        catch (Exception ex)
                        {
                            // Логируем ошибку, но не прерываем основной процесс сохранения профиля
                            _logger.LogError(ex, "Ошибка при попытке обновления ArtistName в треках для User ID {UserId} при смене Username.", userId);
                            // Можно добавить сообщение пользователю, что имена в треках могли не обновиться
                            TempData["WarningMessage"] = "Произошла ошибка при обновлении имени автора в существующих треках.";
                        }
                        // --- ↑↑↑ Конец обновления ArtistName в треках ↑↑↑ ---
                    }
                }

                // --- Проверка и обновление Email ---
                if (userToUpdate.Email != model.Email)
                {
                    bool emailExists = await _context.Users.AnyAsync(u => u.Id != userId && u.Email == model.Email);
                    if (emailExists)
                    {
                        ModelState.AddModelError(nameof(model.Email), "Этот email уже занят.");
                    }
                    else
                    {
                        _logger.LogInformation("Обновление Email для User ID {UserId} с '{OldValue}' на '{NewValue}'", userId, userToUpdate.Email, model.Email);
                        userToUpdate.Email = model.Email;
                        changesMade = true;
                        needsReLogin = true;
                    }
                }

                // --- Проверка и обновление Пароля ---
                if (!string.IsNullOrEmpty(model.NewPassword))
                {
                    _logger.LogInformation("Обновление пароля для User ID {UserId}.", userId);
                    userToUpdate.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword, BCrypt.Net.BCrypt.GenerateSalt(12));
                    changesMade = true;
                }

                // --- Сохранение и результат ---
                if (ModelState.ErrorCount == 0)
                {
                    if (changesMade)
                    {
                        try
                        {
                            await _context.SaveChangesAsync(); // Сохраняем все изменения (User и Tracks)
                            _logger.LogInformation("Изменения профиля для User ID {UserId} успешно сохранены.", userId);
                            TempData["SuccessMessage"] = "Изменения профиля успешно сохранены.";

                            if (needsReLogin)
                            {
                                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                                _logger.LogInformation("User ID {UserId} signed out due to profile changes. Needs re-login.", userId);
                                TempData["SuccessMessage"] += " Пожалуйста, войдите снова.";
                                return RedirectToAction("Login", "Account");
                            }
                            else { return RedirectToAction(nameof(Index)); }
                        }
                        catch (Exception ex) // Обработка ошибок сохранения
                        {
                            _logger.LogError(ex, "Ошибка при SaveChangesAsync для профиля User ID {UserId}.", userId);
                            ModelState.AddModelError("", "Не удалось сохранить изменения.");
                        }
                    }
                    else
                    {
                        TempData["InfoMessage"] = "Изменений не было.";
                        return RedirectToAction(nameof(Index));
                    }
                }
            } // Конец if (ModelState.IsValid)

            // Если модель не валидна или ошибка сохранения, возвращаем форму
            return View(model);
        }
    }
}