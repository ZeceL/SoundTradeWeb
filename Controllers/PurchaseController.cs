using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data;
using SoundTradeWebApp.Enums;
using SoundTradeWebApp.Models;
using SoundTradeWebApp.Models.ViewModels; // Добавлено
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SoundTradeWebApp.Controllers
{
    [Authorize]
    public class PurchaseController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PurchaseController> _logger;

        public PurchaseController(ApplicationDbContext context, ILogger<PurchaseController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Purchase/Confirm/{trackId}
        public async Task<IActionResult> Confirm(int trackId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var track = await _context.Tracks
                .Include(t => t.AuthorUser) // Включаем автора для отображения имени
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == trackId);

            if (track == null)
            {
                TempData["ErrorMessage"] = "Трек не найден.";
                return RedirectToAction("Index", "Catalog");
            }

            if (track.AuthorUserId == currentUserId)
            {
                TempData["WarningMessage"] = "Вы не можете купить собственный трек.";
                return RedirectToAction("Index", "Catalog");
            }

            var viewModel = new ConfirmPurchaseViewModel
            {
                TrackId = track.Id,
                Title = track.Title,
                ArtistName = track.AuthorUser?.Username ?? track.ArtistName, // Отображаем Username текущего владельца
                Genre = track.Genre,
                VocalType = track.VocalType,
                Mood = track.Mood
            };

            return View("Purchase", viewModel); // Указываем имя нового представления
        }


        [HttpPost]
        [ValidateAntiForgeryToken] // Важно для безопасности форм
        public async Task<IActionResult> CompletePurchase(ConfirmPurchaseViewModel model) // Принимаем ViewModel или TrackId
        {
            var buyerId = GetCurrentUserId();
            if (buyerId == 0)
            {
                TempData["ErrorMessage"] = "Пользователь не авторизован.";
                return RedirectToAction("Login", "Account"); // Или куда-то еще
            }

            // Проверка ModelState если ConfirmPurchaseViewModel имеет DataAnnotations
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Некорректные данные для покупки.";
                // Нужно заново загрузить детали трека для отображения, т.к. model может быть неполной
                var trackDetailsForView = await _context.Tracks
                   .Include(t => t.AuthorUser)
                   .AsNoTracking()
                   .FirstOrDefaultAsync(t => t.Id == model.TrackId);

                if (trackDetailsForView == null)
                {
                    TempData["ErrorMessage"] = "Трек не найден при попытке повторно отобразить страницу.";
                    return RedirectToAction("Index", "Catalog");
                }
                // Заполняем модель заново для возврата на страницу подтверждения
                var viewModelForReturn = new ConfirmPurchaseViewModel
                {
                    TrackId = trackDetailsForView.Id,
                    Title = trackDetailsForView.Title,
                    ArtistName = trackDetailsForView.AuthorUser?.Username ?? trackDetailsForView.ArtistName,
                    Genre = trackDetailsForView.Genre,
                    VocalType = trackDetailsForView.VocalType,
                    Mood = trackDetailsForView.Mood
                };
                return View("Purchase", viewModelForReturn);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var trackToPurchase = await _context.Tracks
                    .Include(t => t.AuthorUser) // Original author
                    .FirstOrDefaultAsync(t => t.Id == model.TrackId); // Используем model.TrackId

                if (trackToPurchase == null)
                {
                    await transaction.RollbackAsync();
                    TempData["ErrorMessage"] = "Трек не найден.";
                    return RedirectToAction("Index", "Catalog");
                }

                var originalAuthorId = trackToPurchase.AuthorUserId;
                var originalAuthor = trackToPurchase.AuthorUser;

                if (originalAuthorId == buyerId)
                {
                    await transaction.RollbackAsync();
                    TempData["WarningMessage"] = "Вы не можете купить собственный трек.";
                    // Возвращаем на страницу подтверждения с заполненной моделью
                    var viewModelForReturn = new ConfirmPurchaseViewModel
                    {
                        TrackId = trackToPurchase.Id,
                        Title = trackToPurchase.Title,
                        ArtistName = originalAuthor?.Username ?? trackToPurchase.ArtistName,
                        Genre = trackToPurchase.Genre,
                        VocalType = trackToPurchase.VocalType,
                        Mood = trackToPurchase.Mood
                    };
                    return View("Purchase", viewModelForReturn);
                }

                var buyer = await _context.Users.FindAsync(buyerId);
                if (buyer == null)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError($"Покупатель с ID {buyerId} не найден при попытке покупки трека {model.TrackId}.");
                    TempData["ErrorMessage"] = "Ошибка данных покупателя.";
                    var viewModelForReturn = new ConfirmPurchaseViewModel // Заполняем для возврата
                    {
                        TrackId = trackToPurchase.Id,
                        Title = trackToPurchase.Title,
                        ArtistName = originalAuthor?.Username ?? trackToPurchase.ArtistName,
                        Genre = trackToPurchase.Genre,
                        VocalType = trackToPurchase.VocalType,
                        Mood = trackToPurchase.Mood
                    };
                    return View("Purchase", viewModelForReturn);
                }

                trackToPurchase.AuthorUserId = buyerId;
                trackToPurchase.ArtistName = buyer.Username;

                _context.Tracks.Update(trackToPurchase);

                if (originalAuthor != null)
                {
                    var notificationToSeller = new Notification
                    {
                        RecipientUserId = originalAuthor.Id,
                        Type = NotificationType.TrackSold,
                        Message = $"Ваш трек '{trackToPurchase.Title}' был куплен пользователем '{buyer.Username}'.",
                        IsRead = false,
                        CreatedDate = DateTime.UtcNow,
                        RelatedItemId = trackToPurchase.Id,
                    };
                    _context.Notifications.Add(notificationToSeller);
                }
                else
                {
                    _logger.LogWarning($"Original author (ID: {originalAuthorId}) not found for track {trackToPurchase.Id}. Seller notification not sent.");
                }

                var notificationToBuyer = new Notification
                {
                    RecipientUserId = buyerId,
                    Type = NotificationType.TrackPurchased,
                    Message = $"Вы успешно приобрели трек '{trackToPurchase.Title}'. Теперь он в вашей коллекции.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow,
                    RelatedItemId = trackToPurchase.Id,
                };
                _context.Notifications.Add(notificationToBuyer);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Трек ID {model.TrackId} успешно куплен пользователем ID {buyerId} у пользователя ID {originalAuthorId}.");
                TempData["SuccessMessage"] = "Трек успешно приобретен!";
                return RedirectToAction("Index", "Catalog"); // Или на страницу "Мои треки"
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Ошибка при покупке трека ID {model.TrackId} пользователем ID {buyerId}.");
                TempData["ErrorMessage"] = "Во время покупки произошла ошибка на сервере.";
                // Заполняем модель для возврата на страницу подтверждения в случае ошибки
                var trackDetailsForErrorView = await _context.Tracks
                    .Include(t => t.AuthorUser)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Id == model.TrackId);

                var viewModelForErrorReturn = new ConfirmPurchaseViewModel();
                if (trackDetailsForErrorView != null)
                {
                    viewModelForErrorReturn.TrackId = trackDetailsForErrorView.Id;
                    viewModelForErrorReturn.Title = trackDetailsForErrorView.Title;
                    viewModelForErrorReturn.ArtistName = trackDetailsForErrorView.AuthorUser?.Username ?? trackDetailsForErrorView.ArtistName;
                    viewModelForErrorReturn.Genre = trackDetailsForErrorView.Genre;
                    viewModelForErrorReturn.VocalType = trackDetailsForErrorView.VocalType;
                    viewModelForErrorReturn.Mood = trackDetailsForErrorView.Mood;
                }
                else
                { // Если трек вообще не найден, хотя это маловероятно на данном этапе
                    viewModelForErrorReturn.TrackId = model.TrackId; // Хотя бы ID передадим
                    TempData["ErrorMessage"] += " Не удалось загрузить детали трека для повторного отображения.";
                }
                return View("Purchase", viewModelForErrorReturn);
            }
        }

        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
}