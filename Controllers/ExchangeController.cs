using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data;
using SoundTradeWebApp.Enums;
using SoundTradeWebApp.Models;
using SoundTradeWebApp.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SoundTradeWebApp.Controllers
{
    [Authorize] // Все действия в этом контроллере требуют авторизации
    public class ExchangeController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ExchangeController> _logger;

        public ExchangeController(ApplicationDbContext context, ILogger<ExchangeController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Exchange/InitiateOffer?requestedTrackId=X
        public async Task<IActionResult> InitiateOffer(int requestedTrackId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var requestedTrack = await _context.Tracks
                .FirstOrDefaultAsync(t => t.Id == requestedTrackId);

            if (requestedTrack == null)
            {
                TempData["ErrorMessage"] = "Запрашиваемый трек не найден.";
                return RedirectToAction("Index", "Catalog");
            }

            if (requestedTrack.AuthorUserId == currentUserId)
            {
                TempData["ErrorMessage"] = "Вы не можете предложить обмен на свой собственный трек.";
                return RedirectToAction("Index", "Catalog");
            }

            // Получаем треки текущего пользователя, которые он может предложить
            // Исключаем треки, уже участвующие в активных ПРЕДЛОЖЕНИЯХ от этого пользователя
            // (где он инициатор и статус Pending)
            var userTracksInPendingOffers = await _context.ExchangeOffers
                .Where(eo => eo.InitiatorUserId == currentUserId && eo.Status == ExchangeStatus.Pending)
                .Select(eo => eo.OfferedTrackId)
                .ToListAsync();

            var availableTracksToOffer = await _context.Tracks
                .Where(t => t.AuthorUserId == currentUserId && t.Id != requestedTrackId && !userTracksInPendingOffers.Contains(t.Id))
                .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Title + " (" + t.ArtistName + ")" })
                .ToListAsync();

            if (!availableTracksToOffer.Any())
            {
                TempData["WarningMessage"] = "У вас нет подходящих треков для обмена, или все ваши треки уже предложены в других активных обменах.";
                // Можно вернуть на каталог или показать специальное сообщение
                // return RedirectToAction("Index", "Catalog");
            }

            var viewModel = new InitiateExchangeViewModel
            {
                RequestedTrackId = requestedTrack.Id,
                RequestedTrackTitle = requestedTrack.Title,
                RequestedTrackArtistName = requestedTrack.ArtistName,
                RequestedTrackOwnerId = requestedTrack.AuthorUserId,
                AvailableTracksToOffer = availableTracksToOffer
            };
            viewModel.AvailableTracksToOffer.Insert(0, new SelectListItem { Value = "", Text = "-- Выберите ваш трек --" });


            return View(viewModel);
        }


        // POST: /Exchange/SubmitOffer
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitOffer(InitiateExchangeViewModel model)
        {
            var initiatorUserId = GetCurrentUserId();
            if (initiatorUserId == 0) return Unauthorized();

            if (!ModelState.IsValid) // Проверяем валидность ViewModel (например, выбран ли OfferedTrackId)
            {
                TempData["ErrorMessage"] = "Пожалуйста, выберите трек, который вы предлагаете для обмена.";
                // Нужно перезаполнить список треков для формы
                var requestedTrackInfo = await _context.Tracks.FindAsync(model.RequestedTrackId);
                model.RequestedTrackTitle = requestedTrackInfo?.Title;
                model.RequestedTrackArtistName = requestedTrackInfo?.ArtistName;
                // Перезаполняем список доступных треков
                var userTracksInPendingOffers = await _context.ExchangeOffers
                   .Where(eo => eo.InitiatorUserId == initiatorUserId && eo.Status == ExchangeStatus.Pending)
                   .Select(eo => eo.OfferedTrackId)
                   .ToListAsync();
                model.AvailableTracksToOffer = await _context.Tracks
                    .Where(t => t.AuthorUserId == initiatorUserId && t.Id != model.RequestedTrackId && !userTracksInPendingOffers.Contains(t.Id))
                    .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Title + " (" + t.ArtistName + ")" })
                    .ToListAsync();
                model.AvailableTracksToOffer.Insert(0, new SelectListItem { Value = "", Text = "-- Выберите ваш трек --" });
                return View("InitiateOffer", model);
            }

            // Дополнительные проверки
            var offeredTrack = await _context.Tracks.FirstOrDefaultAsync(t => t.Id == model.OfferedTrackId && t.AuthorUserId == initiatorUserId);
            var requestedTrack = await _context.Tracks.Include(t => t.AuthorUser).FirstOrDefaultAsync(t => t.Id == model.RequestedTrackId);

            if (offeredTrack == null)
            {
                TempData["ErrorMessage"] = "Предлагаемый вами трек не найден или не принадлежит вам.";
                return RedirectToAction(nameof(InitiateOffer), new { requestedTrackId = model.RequestedTrackId });
            }
            if (requestedTrack == null)
            {
                TempData["ErrorMessage"] = "Запрашиваемый трек для обмена не найден.";
                return RedirectToAction("Index", "Catalog");
            }
            if (requestedTrack.AuthorUserId == initiatorUserId)
            {
                TempData["ErrorMessage"] = "Вы не можете обменять трек сам с собой.";
                return RedirectToAction(nameof(InitiateOffer), new { requestedTrackId = model.RequestedTrackId });
            }
            if (model.OfferedTrackId == model.RequestedTrackId)
            {
                TempData["ErrorMessage"] = "Нельзя обменять трек на самого себя.";
                return RedirectToAction(nameof(InitiateOffer), new { requestedTrackId = model.RequestedTrackId });
            }

            // Проверяем, нет ли уже активного идентичного предложения
            bool existingOffer = await _context.ExchangeOffers.AnyAsync(eo =>
                eo.InitiatorUserId == initiatorUserId &&
                eo.OfferedTrackId == model.OfferedTrackId &&
                eo.RecipientUserId == requestedTrack.AuthorUserId &&
                eo.RequestedTrackId == model.RequestedTrackId &&
                eo.Status == ExchangeStatus.Pending);

            if (existingOffer)
            {
                TempData["WarningMessage"] = "Вы уже отправили такое же предложение обмена. Ожидайте ответа.";
                return RedirectToAction("Index", "Catalog");
            }

            // Все проверки пройдены, создаем предложение обмена
            var exchangeOffer = new ExchangeOffer
            {
                InitiatorUserId = initiatorUserId,
                OfferedTrackId = model.OfferedTrackId,
                RecipientUserId = requestedTrack.AuthorUserId, // Владелец запрашиваемого трека
                RequestedTrackId = model.RequestedTrackId,
                Status = ExchangeStatus.Pending,
                OfferDate = DateTime.UtcNow
            };
            _context.ExchangeOffers.Add(exchangeOffer);
            // await _context.SaveChangesAsync(); // Сохраним позже вместе с уведомлением, если возможно в одной транзакции

            // Создаем уведомление для получателя предложения
            var recipientUser = requestedTrack.AuthorUser; // Уже загружен через Include
            var initiatorUser = await _context.Users.FindAsync(initiatorUserId); // Загружаем инициатора для имени

            if (recipientUser != null && initiatorUser != null)
            {
                var notification = new Notification
                {
                    RecipientUserId = recipientUser.Id,
                    Type = NotificationType.ExchangeOfferReceived,
                    Message = $"Пользователь '{initiatorUser.Username}' предложил вам обмен: " +
                              $"его трек '{offeredTrack.Title}' ({offeredTrack.ArtistName}) на " +
                              $"ваш трек '{requestedTrack.Title}' ({requestedTrack.ArtistName}).",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow,
                    // LinkUrl будет сформирован после сохранения exchangeOffer, чтобы получить его ID
                };
                _context.Notifications.Add(notification);

                // Сохраняем все изменения (предложение и уведомление)
                await _context.SaveChangesAsync();

                // Теперь, когда у exchangeOffer есть ID, обновляем LinkUrl у уведомления
                notification.RelatedItemId = exchangeOffer.Id;
                notification.LinkUrl = Url.Action("ViewOffer", "Exchange", new { offerId = exchangeOffer.Id });
                await _context.SaveChangesAsync(); // Сохраняем обновление LinkUrl

                _logger.LogInformation("Предложение обмена ID {OfferId} от User {InitiatorId} к User {RecipientId} создано. Уведомление ID {NotificationId} отправлено.",
                                      exchangeOffer.Id, initiatorUserId, recipientUser.Id, notification.Id);
                TempData["SuccessMessage"] = "Предложение обмена успешно отправлено!";
            }
            else
            {
                _logger.LogError("Не удалось найти пользователя-получателя или инициатора для создания уведомления об обмене.");
                TempData["ErrorMessage"] = "Предложение создано, но не удалось отправить уведомление получателю.";
                // Если здесь ошибка, стоит откатить создание ExchangeOffer или обработать иначе
            }

            return RedirectToAction("Index", "Catalog");
        }

        // GET: /Exchange/ViewOffer/{offerId}
        public async Task<IActionResult> ViewOffer(int offerId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var offer = await _context.ExchangeOffers
                .Include(eo => eo.Initiator)
                .Include(eo => eo.Recipient)
                .Include(eo => eo.OfferedTrack)
                .Include(eo => eo.RequestedTrack)
                .FirstOrDefaultAsync(eo => eo.Id == offerId);

            if (offer == null)
            {
                TempData["ErrorMessage"] = "Предложение обмена не найдено.";
                return RedirectToAction("Index", "Notification"); // Или на главную
            }

            // Проверка, что текущий пользователь либо инициатор, либо получатель
            if (offer.InitiatorUserId != currentUserId && offer.RecipientUserId != currentUserId)
            {
                TempData["ErrorMessage"] = "У вас нет доступа к этому предложению обмена.";
                return RedirectToAction("Index", "Notification");
            }

            // Помечаем уведомление как прочитанное, если оно связано с этим предложением и адресовано текущему пользователю
            var relatedNotification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.RecipientUserId == currentUserId &&
                                          n.RelatedItemId == offerId &&
                                          n.Type == NotificationType.ExchangeOfferReceived && // Только для первоначального предложения
                                          !n.IsRead);
            if (relatedNotification != null)
            {
                relatedNotification.IsRead = true;
                await _context.SaveChangesAsync();
            }

            var viewModel = new ViewExchangeOfferViewModel
            {
                OfferId = offer.Id,
                OfferedTrackId = offer.OfferedTrackId,
                OfferedTrackTitle = offer.OfferedTrack?.Title,
                OfferedTrackArtistName = offer.OfferedTrack?.ArtistName, // Изначальный артист предложенного трека
                OfferedTrackAudioUrl = Url.Action("GetAudio", "Tracks", new { id = offer.OfferedTrackId }),

                RequestedTrackId = offer.RequestedTrackId,
                RequestedTrackTitle = offer.RequestedTrack?.Title,
                RequestedTrackArtistName = offer.RequestedTrack?.ArtistName, // Изначальный артист запрашиваемого трека
                RequestedTrackAudioUrl = Url.Action("GetAudio", "Tracks", new { id = offer.RequestedTrackId }),

                InitiatorUserId = offer.InitiatorUserId,
                InitiatorUsername = offer.Initiator?.Username,
                RecipientUserId = offer.RecipientUserId,
                Status = offer.Status,
                OfferDate = offer.OfferDate.ToLocalTime(),
                ResponseDate = offer.ResponseDate?.ToLocalTime(),
                CanRespond = offer.Status == ExchangeStatus.Pending && offer.RecipientUserId == currentUserId
            };

            return View(viewModel);
        }

        // POST: /Exchange/AcceptOffer/{offerId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptOffer(int offerId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            // Начинаем транзакцию БД
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                var offer = await _context.ExchangeOffers
                    .Include(eo => eo.OfferedTrack)  // Нужен для обновления
                    .Include(eo => eo.RequestedTrack) // Нужен для обновления
                    .Include(eo => eo.Initiator)      // Нужен для уведомления
                    .Include(eo => eo.Recipient)      // Нужен для уведомления
                    .FirstOrDefaultAsync(eo => eo.Id == offerId &&
                                           eo.RecipientUserId == currentUserId &&
                                           eo.Status == ExchangeStatus.Pending);

                if (offer == null)
                {
                    TempData["ErrorMessage"] = "Предложение для принятия не найдено или уже обработано.";
                    await transaction.RollbackAsync();
                    return RedirectToAction("Index", "Notification");
                }

                // --- Производим обмен треками ---
                var trackOfferedByInitiator = offer.OfferedTrack; // Трек, который отдаёт инициатор
                var trackRequestedFromRecipient = offer.RequestedTrack; // Трек, который отдаёт получатель (текущий юзер)

                if (trackOfferedByInitiator == null || trackRequestedFromRecipient == null)
                {
                    TempData["ErrorMessage"] = "Один из треков для обмена не найден. Обмен невозможен.";
                    _logger.LogError("Ошибка обмена: трек не найден для OfferId {OfferId}. OfferedTrackId: {OTrId}, RequestedTrackId: {RTrId}",
                        offer.Id, offer.OfferedTrackId, offer.RequestedTrackId);
                    await transaction.RollbackAsync();
                    return RedirectToAction("Index", "Notification");
                }

                // Сохраняем текущих владельцев и их имена (username) для корректного присвоения ArtistName
                var initiator = offer.Initiator; // Пользователь, который ОТДАЁТ offeredTrack и ПОЛУЧАЕТ requestedTrack
                var recipient = offer.Recipient; // Пользователь, который ОТДАЁТ requestedTrack и ПОЛУЧАЕТ offeredTrack (текущий юзер)

                if (initiator == null || recipient == null)
                {
                    TempData["ErrorMessage"] = "Ошибка данных пользователей для обмена.";
                    _logger.LogError("Ошибка обмена: пользователь не найден для OfferId {OfferId}. InitiatorId: {InitId}, RecipientId: {RecId}",
                       offer.Id, offer.InitiatorUserId, offer.RecipientUserId);
                    await transaction.RollbackAsync();
                    return RedirectToAction("Index", "Notification");
                }

                // Меняем владельцев
                // Трек, который раньше принадлежал инициатору (trackOfferedByInitiator), теперь принадлежит получателю (recipient)
                trackOfferedByInitiator.AuthorUserId = recipient.Id;
                trackOfferedByInitiator.ArtistName = recipient.Username; // Имя артиста становится именем нового владельца

                // Трек, который раньше принадлежал получателю (trackRequestedFromRecipient), теперь принадлежит инициатору (initiator)
                trackRequestedFromRecipient.AuthorUserId = initiator.Id;
                trackRequestedFromRecipient.ArtistName = initiator.Username;

                // Обновляем статус предложения
                offer.Status = ExchangeStatus.Accepted;
                offer.ResponseDate = DateTime.UtcNow;

                // Создаем уведомления
                // 1. Уведомление инициатору, что его предложение принято
                var notificationToInitiator = new Notification
                {
                    RecipientUserId = offer.InitiatorUserId,
                    Type = NotificationType.ExchangeOfferAccepted,
                    Message = $"Ваше предложение обменять трек '{trackOfferedByInitiator.Title}' на '{trackRequestedFromRecipient.Title}' было принято пользователем '{recipient.Username}'. Треки успешно обменены.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow,
                    RelatedItemId = offer.Id,
                    LinkUrl = Url.Action("ViewOffer", "Exchange", new { offerId = offer.Id })
                };
                _context.Notifications.Add(notificationToInitiator);

                // 2. Уведомление получателю (текущему пользователю), что обмен завершен
                var notificationToRecipient = new Notification
                {
                    RecipientUserId = offer.RecipientUserId,
                    Type = NotificationType.ExchangeCompleted,
                    Message = $"Вы приняли предложение обмена от '{initiator.Username}'. Трек '{trackRequestedFromRecipient.Title}' теперь принадлежит ему, а трек '{trackOfferedByInitiator.Title}' - вам.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow,
                    RelatedItemId = offer.Id,
                    LinkUrl = Url.Action("ViewOffer", "Exchange", new { offerId = offer.Id })
                };
                _context.Notifications.Add(notificationToRecipient);

                // Сохраняем все изменения (треки, предложение, уведомления)
                await _context.SaveChangesAsync();
                await transaction.CommitAsync(); // Подтверждаем транзакцию

                _logger.LogInformation("Обмен OfferId {OfferId} принят. Треки {Track1Id} и {Track2Id} обменены между User {User1Id} и User {User2Id}",
                    offer.Id, trackOfferedByInitiator.Id, trackRequestedFromRecipient.Id, initiator.Id, recipient.Id);
                TempData["SuccessMessage"] = "Обмен успешно совершен!";

            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Ошибка при принятии предложения обмена OfferId {OfferId} пользователем {UserId}.", offerId, currentUserId);
                TempData["ErrorMessage"] = "Произошла ошибка при принятии обмена.";
            }
            return RedirectToAction("Index", "Notification");
        }


        // POST: /Exchange/DeclineOffer/{offerId}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeclineOffer(int offerId)
        {
            var currentUserId = GetCurrentUserId();
            if (currentUserId == 0) return Unauthorized();

            var offer = await _context.ExchangeOffers
                .Include(eo => eo.Initiator) // Нужен для уведомления
                .Include(eo => eo.Recipient) // Для проверки
                .Include(eo => eo.OfferedTrack) // Для сообщения
                .Include(eo => eo.RequestedTrack) // Для сообщения
                .FirstOrDefaultAsync(eo => eo.Id == offerId &&
                                       eo.RecipientUserId == currentUserId &&
                                       eo.Status == ExchangeStatus.Pending);

            if (offer == null)
            {
                TempData["ErrorMessage"] = "Предложение для отклонения не найдено или уже обработано.";
                return RedirectToAction("Index", "Notification");
            }

            offer.Status = ExchangeStatus.Declined;
            offer.ResponseDate = DateTime.UtcNow;

            // Уведомление инициатору, что его предложение отклонено
            if (offer.Initiator != null && offer.OfferedTrack != null && offer.RequestedTrack != null && offer.Recipient != null)
            {
                var notificationToInitiator = new Notification
                {
                    RecipientUserId = offer.InitiatorUserId,
                    Type = NotificationType.ExchangeOfferDeclined,
                    Message = $"Ваше предложение обменять трек '{offer.OfferedTrack.Title}' на '{offer.RequestedTrack.Title}' было отклонено пользователем '{offer.Recipient.Username}'.",
                    IsRead = false,
                    CreatedDate = DateTime.UtcNow,
                    RelatedItemId = offer.Id,
                    LinkUrl = Url.Action("ViewOffer", "Exchange", new { offerId = offer.Id })
                };
                _context.Notifications.Add(notificationToInitiator);
            }
            else
            {
                _logger.LogWarning("Не удалось создать уведомление об отклонении OfferId {OfferId} из-за отсутствия данных.", offerId);
            }


            await _context.SaveChangesAsync();

            _logger.LogInformation("Обмен OfferId {OfferId} отклонен пользователем {UserId}", offer.Id, currentUserId);
            TempData["InfoMessage"] = "Предложение обмена отклонено.";
            return RedirectToAction("Index", "Notification");
        }

        // Вспомогательный метод для получения ID текущего пользователя
        private int GetCurrentUserId()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(userIdString, out int userId))
            {
                return userId;
            }
            return 0; // Означает, что пользователь не найден или не аутентифицирован должным образом
        }
    }
}