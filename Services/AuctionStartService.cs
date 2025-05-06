using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options; // Для IOptions
using SoundTradeWebApp.Data;
using SoundTradeWebApp.Enums;
using SoundTradeWebApp.Models;
using SoundTradeWebApp.Models.Configuration; // Для AuctionSettings
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace SoundTradeWebApp.Services
{
    public class AuctionStartService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuctionStartService> _logger;
        private readonly AuctionSettings _settings;

        public AuctionStartService(IServiceScopeFactory scopeFactory,
                                   IOptions<AuctionSettings> settings, // Внедряем настройки
                                   ILogger<AuctionStartService> logger)
        {
            _scopeFactory = scopeFactory;
            _settings = settings.Value; // Получаем объект настроек
            _logger = logger;
            _logger.LogInformation("AuctionStartService создан. InitialDelay={Delay}, CheckInterval={Interval}, Duration={Duration}",
                                   _settings.InitialDelay, _settings.CheckInterval, _settings.AuctionDuration);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AuctionStartService запущен.");
            try // Внешний try для перехвата OperationCanceledException и критических ошибок СТАРТА
            {
                _logger.LogInformation("Ожидание начальной задержки: {InitialDelay}", _settings.InitialDelay);
                await Task.Delay(_settings.InitialDelay, stoppingToken);

                // Основной цикл проверки
                while (!stoppingToken.IsCancellationRequested)
                {
                    _logger.LogInformation("AuctionStartService: Начало цикла проверки.");
                    try // Внутренний try для КАЖДОЙ итерации проверки/обработки
                    {
                        using (var scope = _scopeFactory.CreateScope())
                        {
                            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                            var scopedLogger = scope.ServiceProvider.GetRequiredService<ILogger<AuctionStartService>>();
                            var now = DateTime.UtcNow;

                            // --- 1. ПРОВЕРКА И ЗАПУСК НОВОГО АУКЦИОНА ---
                            bool isAuctionActive = await dbContext.Auctions
                                .AnyAsync(a => a.Status == AuctionStatus.Active && a.EndTime > now, stoppingToken);

                            if (!isAuctionActive) // Блок для ЗАПУСКА аукциона
                            {
                                scopedLogger.LogInformation("Активный аукцион не найден. Поиск заявок...");
                                // pendingSubmissions объявлена ЗДЕСЬ
                                var pendingSubmissions = await dbContext.AuctionSubmissions
                                    .Where(s => s.Status == AuctionStatus.Pending)
                                    .ToListAsync(stoppingToken);

                                if (pendingSubmissions.Any()) // Используем pendingSubmissions ЗДЕСЬ
                                {
                                    scopedLogger.LogInformation("Найдено {Count} ожидающих заявок. Выбор случайной...", pendingSubmissions.Count); // Используем pendingSubmissions ЗДЕСЬ
                                    int randomIndex = Random.Shared.Next(pendingSubmissions.Count); // Используем pendingSubmissions ЗДЕСЬ
                                    var selectedSubmission = pendingSubmissions[randomIndex]; // Используем pendingSubmissions ЗДЕСЬ

                                    // Загружаем трек ВМЕСТО AnyAsync
                                    var trackToAuction = await dbContext.Tracks
                                                              .FirstOrDefaultAsync(t => t.Id == selectedSubmission.TrackId, stoppingToken);

                                    if (trackToAuction == null)
                                    {
                                        scopedLogger.LogWarning("Трек {TrackId} для выбранной заявки {SubmissionId} НЕ НАЙДЕН в БД. Заявка будет отменена.",
                                                             selectedSubmission.TrackId, selectedSubmission.Id);
                                        selectedSubmission.Status = AuctionStatus.SubmissionCancelled;
                                        await dbContext.SaveChangesAsync(stoppingToken); // Сохраняем статус заявки
                                                                                         // continue; // Пропускаем остаток итерации цикла -> ПЕРЕНЕСЕНО ВНЕ if/else
                                    }
                                    else
                                    {
                                        // Если трек найден, создаем аукцион
                                        scopedLogger.LogInformation("Трек {TrackId} ('{TrackTitle}') найден. Создание аукциона...",
                                               trackToAuction.Id, trackToAuction.Title);

                                        var auctionStartTime = DateTime.UtcNow; // Фиксируем время старта
                                        var newAuction = new Auction
                                        {
                                            TrackId = trackToAuction.Id,
                                            StartTime = auctionStartTime,
                                            EndTime = auctionStartTime.Add(_settings.AuctionDuration),
                                            StartingBid = selectedSubmission.StartingBid,
                                            CurrentHighestBid = null,
                                            CurrentHighestBidderUserId = null,
                                            BidIncrement = selectedSubmission.BidIncrement,
                                            Status = AuctionStatus.Active // Сразу Активный
                                        };
                                        selectedSubmission.Status = AuctionStatus.SelectedForAuction;

                                        dbContext.Auctions.Add(newAuction);

                                        scopedLogger.LogInformation("Попытка SaveChangesAsync для нового Auction (TrackId: {TrackId}) и Submission {SubmissionId}", newAuction.TrackId, selectedSubmission.Id);
                                        await dbContext.SaveChangesAsync(stoppingToken); // Сохраняем АУКЦИОН и ЗАЯВКУ

                                        scopedLogger.LogInformation("Аукцион ID {AuctionId} для трека {TrackId} успешно создан и запущен. Заявка {SubmissionId} обновлена.",
                                            newAuction.Id, newAuction.TrackId, selectedSubmission.Id);
                                    } // Конец if (trackToAuction == null) ... else
                                } // Конец if (pendingSubmissions.Any())
                                else
                                {
                                    scopedLogger.LogInformation("Ожидающие заявки не найдены.");
                                }
                            } // Конец if (!isAuctionActive)
                            else
                            {
                                scopedLogger.LogInformation("Найден активный аукцион. Запуск нового не требуется.");
                            }

                            // --- 2. ПРОВЕРКА И ЗАВЕРШЕНИЕ СТАРЫХ АУКЦИОНОВ ---
                            // Эта логика выполняется НЕЗАВИСИМО от того, был ли запущен новый аукцион
                            scopedLogger.LogInformation("Поиск аукционов для завершения...");
                            var auctionsToFinish = await dbContext.Auctions
                                .Include(a => a.Track)
                                .Include(a => a.CurrentHighestBidder) // Включаем победителя
                                .Where(a => a.Status == AuctionStatus.Active && a.EndTime <= now)
                                .ToListAsync(stoppingToken);

                            if (auctionsToFinish.Any())
                            {
                                scopedLogger.LogInformation("Найдено {Count} аукционов для завершения.", auctionsToFinish.Count);
                                foreach (var auction in auctionsToFinish)
                                {
                                    // ... (Логика завершения аукциона и смены владельца трека - БЕЗ ИЗМЕНЕНИЙ) ...
                                    scopedLogger.LogInformation("Завершение аукциона ID: {AuctionId} для трека ID: {TrackId}", auction.Id, auction.TrackId);
                                    auction.Status = AuctionStatus.Finished;
                                    auction.WinnerUserId = auction.CurrentHighestBidderUserId;

                                    if (auction.WinnerUserId.HasValue && auction.Track != null && auction.CurrentHighestBidder != null)
                                    {
                                        var winner = auction.CurrentHighestBidder;
                                        var track = auction.Track; // Трек уже загружен через Include
                                        scopedLogger.LogWarning("Передача прав на трек ID {TrackId} от User ID {OldOwnerId} к User ID {NewOwnerId} ({NewOwnerUsername}). Ставка: {WinningBid}",
                                            track.Id, track.AuthorUserId, winner.Id, winner.Username, auction.CurrentHighestBid);
                                        track.AuthorUserId = winner.Id;
                                        track.ArtistName = winner.Username;
                                    }
                                    else { scopedLogger.LogInformation("Аукцион ID {AuctionId} завершен без ставок или победителя.", auction.Id); }
                                }
                                // Сохраняем ИЗМЕНЕНИЯ СТАТУСОВ аукционов и владельцев треков
                                await dbContext.SaveChangesAsync(stoppingToken);
                                scopedLogger.LogInformation("Статусы завершенных аукционов и владельцы треков обновлены.");
                            }
                            else
                            {
                                scopedLogger.LogInformation("Нет аукционов для завершения.");
                            }

                        } // Конец using (scope)
                    }
                    // Внутренний catch - ловит ошибки КАЖДОЙ итерации цикла
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogError(ex, "Ошибка в цикле проверки AuctionStartService.");
                        // НЕ ПРЕРЫВАЕМ ЦИКЛ, просто логируем и ждем следующей итерации
                    }

                    // Ожидание перед следующей проверкой (ВНУТРИ while, ПОСЛЕ внутреннего try-catch)
                    _logger.LogInformation("AuctionStartService: Ожидание следующей проверки через {CheckInterval}", _settings.CheckInterval);
                    await Task.Delay(_settings.CheckInterval, stoppingToken);

                } // Конец while (!stoppingToken.IsCancellationRequested)
            }
            // Внешний catch для OperationCanceledException
            catch (OperationCanceledException)
            {
                _logger.LogInformation("AuctionStartService останавливается (запрошена отмена).");
            }
            // Внешний КРИТИЧЕСКИЙ catch - ловит ошибки ИНИЦИАЛИЗАЦИИ или первого Task.Delay
            // УДАЛЕН блок catch(Exception ex) для исправления CS0160
            // catch (Exception ex)
            // {
            //     _logger.LogCritical(ex, "Критическая ошибка в AuctionStartService (возможно, при старте).");
            // }

            _logger.LogInformation("AuctionStartService остановлен.");
        } // Конец ExecuteAsync
    }
}