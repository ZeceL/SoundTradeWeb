using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data;
using SoundTradeWebApp.Enums; // Для AuctionStatus
using SoundTradeWebApp.Models;
using SoundTradeWebApp.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SoundTradeWebApp.Controllers
{
    [Authorize] // Доступ только авторизованным пользователям
    public class AuctionController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AuctionController> _logger;

        public AuctionController(ApplicationDbContext context, ILogger<AuctionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: /Auction/Index
        public async Task<IActionResult> Index()
        {
            var viewModel = new AuctionIndexViewModel();
            var now = DateTime.UtcNow; // Текущее время для сравнения

            // 1. Ищем активный аукцион
            var activeAuction = await _context.Auctions
                .Include(a => a.Track) // Включаем данные трека
                .Include(a => a.CurrentHighestBidder) // Включаем данные текущего лидера
                .Where(a => a.Status == AuctionStatus.Active && a.EndTime > now)
                .OrderBy(a => a.StartTime) // На случай, если вдруг их несколько (берем самый старый активный)
                .FirstOrDefaultAsync();

            if (activeAuction != null && activeAuction.Track != null) // Убедимся, что трек подгрузился
            {
                viewModel.IsAuctionActive = true;

                // Заполняем детали аукциона
                var details = new AuctionDetailsViewModel
                {
                    AuctionId = activeAuction.Id,
                    TrackId = activeAuction.TrackId,
                    TrackTitle = activeAuction.Track.Title,
                    ArtistName = activeAuction.Track.ArtistName,
                    AudioUrl = Url.Action("GetAudio", "Tracks", new { id = activeAuction.TrackId }) ?? "",
                    StartingBid = activeAuction.StartingBid,
                    CurrentBid = activeAuction.CurrentHighestBid,
                    HighestBidderUsername = activeAuction.CurrentHighestBidder?.Username ?? (activeAuction.CurrentHighestBid.HasValue ? "Ставка есть" : "Ставок еще нет"),
                    BidIncrement = activeAuction.BidIncrement,
                    EndTime = activeAuction.EndTime.ToLocalTime(), // Показываем локальное время окончания
                    TimeRemaining = activeAuction.EndTime > now ? activeAuction.EndTime - now : TimeSpan.Zero,
                    Status = activeAuction.Status,
                    // Рассчитываем следующую минимальную ставку
                    NextMinimumBid = (activeAuction.CurrentHighestBid ?? activeAuction.StartingBid) + activeAuction.BidIncrement
                };
                // TODO: Загрузить последние ставки, если нужно
                // details.RecentBids = ...

                viewModel.CurrentAuction = details;
                _logger.LogInformation("Отображается активный аукцион ID {AuctionId} для трека {TrackId}", activeAuction.Id, activeAuction.TrackId);
            }
            else // Если активного аукциона нет
            {
                viewModel.IsAuctionActive = false;
                _logger.LogInformation("Активный аукцион не найден.");

                // Проверяем, может ли автор подать заявку
                if (User.IsInRole("Author"))
                {
                    var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    if (int.TryParse(userIdString, out int userId))
                    {
                        // TODO: Улучшить проверку возможности подачи заявки
                        viewModel.CanSubmitTrack = true;
                        viewModel.NewSubmission = new AuctionSubmissionViewModel();
                        await PopulateSubmissionDropdowns(viewModel.NewSubmission, userId);
                    }
                }
            }

            return View(viewModel);
        }

        // POST: /Auction/SubmitTrack - Подача заявки автором
        [HttpPost]
        [Authorize(Roles = "Author")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitTrack([Bind(Prefix = "NewSubmission")] AuctionSubmissionViewModel model) // Принимаем МОДЕЛЬ ФОРМЫ
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int userId = 0; // Инициализация
            var authorUsername = User.Identity?.Name ?? "Unknown"; // Имя для логов

            // Проверка ID пользователя
            if (!int.TryParse(userIdString, out userId) || string.IsNullOrEmpty(authorUsername))
            {
                // Критическая ошибка - добавляем в ModelState и возвращаем вид
                ModelState.AddModelError("", "Ошибка идентификации пользователя. Попробуйте войти снова.");
                // Нужно подготовить ViewModel для Index
                var errorViewModel = new AuctionIndexViewModel { CanSubmitTrack = false }; // Не даем отправить снова без перезагрузки
                return View("Index", errorViewModel); // Возвращаем главный вид Index
            }

            _logger.LogInformation("SubmitTrack POST received. UserID: {UserId}, TrackId: {TrackId}, StartingBid: {StartingBid}, Increment: {Increment}",
                userId, model.TrackId, model.StartingBid, model.BidIncrement);

            // Ручная проверка шага ставки (если не используем Range или хотим доп. проверку)
            if (!IsValidBidIncrement(model.BidIncrement))
            {
                ModelState.AddModelError(nameof(model.BidIncrement), "Выбран некорректный шаг ставки (500, 1000 или 2000).");
            }

            // Проверка принадлежности трека и доступности (упрощенная)
            bool trackIsValid = false;
            if (model.TrackId > 0) // Проверяем только если ID > 0
            {
                trackIsValid = await _context.Tracks.AnyAsync(t => t.Id == model.TrackId && t.AuthorUserId == userId);
                // TODO: Добавить проверку, что трек не в активной заявке/аукционе
            }
            if (!trackIsValid && model.TrackId > 0) // Если ID был, но трек невалидный
            {
                ModelState.AddModelError(nameof(model.TrackId), "Выбран недопустимый трек (не ваш?).");
            }
            // Если TrackId == 0, ошибка "Необходимо выбрать трек" добавится атрибутом Range

            // Теперь проверяем ModelState ПОСЛЕ всех ручных добавлений ошибок
            if (ModelState.IsValid)
            {
                // --- Блок try...catch для сохранения в БД ---
                try
                {
                    var submission = new AuctionSubmission
                    {
                        TrackId = model.TrackId,
                        OriginalAuthorUserId = userId,
                        StartingBid = model.StartingBid,
                        BidIncrement = model.BidIncrement,
                        SubmissionTime = DateTime.UtcNow,
                        Status = AuctionStatus.Pending
                    };

                    _context.AuctionSubmissions.Add(submission);
                    await _context.SaveChangesAsync();

                    _logger.LogInformation("Автор {UserId} подал заявку на аукцион для трека {TrackId}.", userId, model.TrackId);
                    TempData["SuccessMessage"] = "Ваш трек успешно предложен на аукцион!";
                    return RedirectToAction(nameof(Index)); // Успех -> Редирект
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при сохранении заявки на аукцион от пользователя {UserId} для трека {TrackId}", userId, model.TrackId);
                    // Добавляем ошибку уровня модели
                    ModelState.AddModelError("", "Не удалось подать заявку из-за внутренней ошибки. Попробуйте позже.");
                    // НЕ ДЕЛАЕМ РЕДИРЕКТ, чтобы сохранить ModelState
                }
                // --- Конец try...catch ---
            }

            // --- Если ModelState НЕ IsValid (или была ошибка в catch) ---
            _logger.LogWarning("Невалидная модель при подаче заявки на аукцион от пользователя {UserId}. Ошибки: {Errors}", userId,
                string.Join("; ", ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))));

            // Нужно ПЕРЕСОЗДАТЬ главную ViewModel для страницы Index
            var indexViewModelOnError = new AuctionIndexViewModel
            {
                IsAuctionActive = false, // Мы здесь, значит активного аукциона нет
                CanSubmitTrack = true,   // Даем шанс исправить форму
                NewSubmission = model    // <<-- ПЕРЕДАЕМ СЮДА МОДЕЛЬ С ОШИБКАМИ ModelState
            };
            // И ПЕРЕЗАПОЛНИТЬ ВЫПАДАЮЩИЕ СПИСКИ для этой модели!
            await PopulateSubmissionDropdowns(indexViewModelOnError.NewSubmission, userId);

            // Возвращаем ПРЕДСТАВЛЕНИЕ "Index" с ГЛАВНОЙ ViewModel, содержащей ошибки
            return View("Index", indexViewModelOnError);
        }

        // --- Вспомогательные методы ---
        private async Task PopulateSubmissionDropdowns(AuctionSubmissionViewModel? submissionModel, int userId)
        {
            if (submissionModel == null) return; // Проверка на null

            // TODO: Фильтровать треки, которые уже поданы/на аукционе
            var availableTracks = await _context.Tracks
               .AsNoTracking()
               .Where(t => t.AuthorUserId == userId)
               .OrderBy(t => t.Title)
               .Select(t => new SelectListItem { Value = t.Id.ToString(), Text = t.Title + " (" + t.ArtistName + ")" })
               .ToListAsync();

            submissionModel.AvailableTracks = availableTracks;
            submissionModel.AvailableTracks.Insert(0, new SelectListItem { Value = "", Text = "-- Выберите трек --" }); // Убедимся, что Value=""

            submissionModel.AvailableIncrements = new List<SelectListItem> {
        new() { Value = "", Text = "-- Выберите шаг --" }, // Убедимся, что Value=""
        new() { Value = "500", Text = "500 руб." },
        new() { Value = "1000", Text = "1000 руб." },
        new() { Value = "2000", Text = "2000 руб." } };
        }

        private bool IsValidBidIncrement(int increment)
        {
            return increment == 500 || increment == 1000 || increment == 2000;
        }

        // POST: /Auction/PlaceBid - Размещение новой ставки
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Ставку может делать только авторизованный пользователь
        public async Task<IActionResult> PlaceBid(PlaceBidViewModel model)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int userId))
            {
                TempData["ErrorMessage"] = "Ошибка идентификации пользователя. Войдите снова.";
                return RedirectToAction(nameof(Index)); // Лучше на Index, а не Login
            }

            // Начинаем транзакцию БД для безопасного обновления ставки
            using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable); // Строгий уровень изоляции

            try
            {
                // Находим АКТИВНЫЙ аукцион внутри транзакции
                // Важно: Может потребоваться блокировка строки (зависит от СУБД и уровня конкуренции)
                // Для простоты пока используем стандартный FirstOrDefaultAsync в транзакции
                var auction = await _context.Auctions
                                      .Include(a => a.Track) // Нужен OriginalAuthorUserId
                                      .FirstOrDefaultAsync(a => a.Id == model.AuctionId &&
                                                           a.Status == AuctionStatus.Active &&
                                                           a.EndTime > DateTime.UtcNow);

                // Проверки:
                if (auction == null)
                {
                    TempData["ErrorMessage"] = "Аукцион не найден, завершен или неактивен.";
                    await transaction.RollbackAsync(); // Откатываем транзакцию
                    return RedirectToAction(nameof(Index));
                }

                // Заменяем OriginalAuthorUserId на AuthorUserId
                if (auction.Track?.AuthorUserId == userId) // Сравниваем с ID текущего владельца трека
                {
                    TempData["ErrorMessage"] = "Владелец трека не может делать ставки на свой трек."; // Уточнили сообщение
                    await transaction.RollbackAsync();
                    return RedirectToAction(nameof(Index));
                }

                decimal currentBid = auction.CurrentHighestBid ?? auction.StartingBid; // Определяем текущую ставку (или начальную, если ставок нет)
                decimal nextMinimumBid = currentBid + auction.BidIncrement;

                if (model.BidAmount < nextMinimumBid)
                {
                    TempData["ErrorMessage"] = $"Ваша ставка ({model.BidAmount:N2} руб.) меньше минимально допустимой ({nextMinimumBid:N2} руб.).";
                    await transaction.RollbackAsync();
                    return RedirectToAction(nameof(Index));
                }

                // --- Если все проверки пройдены ---

                // 1. Создаем запись о новой ставке
                var newBid = new Bid
                {
                    AuctionId = auction.Id,
                    BidderUserId = userId,
                    BidAmount = model.BidAmount,
                    BidTime = DateTime.UtcNow
                };
                _context.Bids.Add(newBid);

                // 2. Обновляем данные аукциона
                auction.CurrentHighestBid = model.BidAmount;
                auction.CurrentHighestBidderUserId = userId;

                // 3. Сохраняем изменения (ставки и аукциона)
                await _context.SaveChangesAsync();

                // 4. Подтверждаем транзакцию
                await transaction.CommitAsync();

                _logger.LogInformation("Пользователь {UserId} сделал ставку {Amount} на аукцион {AuctionId}", userId, model.BidAmount, auction.Id);
                TempData["SuccessMessage"] = $"Ваша ставка {model.BidAmount:N2} руб. принята!";

            }
            catch (DbUpdateConcurrencyException ex) // Ошибка одновременного обновления (кто-то успел раньше)
            {
                await transaction.RollbackAsync();
                _logger.LogWarning(ex, "Ошибка конкуренции при размещении ставки на аукцион {AuctionId} пользователем {UserId}.", model.AuctionId, userId);
                TempData["ErrorMessage"] = "Не удалось разместить ставку, так как кто-то сделал ставку раньше. Попробуйте снова.";
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync(); // Откатываем при любой ошибке
                _logger.LogError(ex, "Ошибка при размещении ставки на аукцион {AuctionId} пользователем {UserId}.", model.AuctionId, userId);
                TempData["ErrorMessage"] = "Произошла ошибка при размещении ставки.";
            }

            // Возвращаемся на страницу аукциона
            return RedirectToAction(nameof(Index));
        }
    }
}