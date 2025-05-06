namespace SoundTradeWebApp.Enums
{
    public enum AuctionStatus
    {
        // --- Статусы для Заявок (AuctionSubmission) ---
        Pending,          // Ожидает рассмотрения (подана автором)
        SelectedForAuction, // Выбрана для следующего аукциона
        NotSelected,      // Не выбрана (проиграла случайный выбор)
        SubmissionCancelled, // Заявка отменена автором (если разрешим)

        // --- Статусы для Аукционов (Auction) ---
        Scheduled,        // Запланирован (еще не начался)
        Active,           // Идет прямо сейчас
        Finished,         // Завершен (время вышло)
        Cancelled         // Отменен администратором или по другой причине
    }
}