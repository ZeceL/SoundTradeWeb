namespace SoundTradeWebApp.Enums
{
    public enum NotificationType
    {
        // Для обменов
        ExchangeOfferReceived,    // Получено новое предложение обмена
        ExchangeOfferAccepted,    // Ваше предложение обмена принято
        ExchangeOfferDeclined,    // Ваше предложение обмена отклонено
        ExchangeCompleted,        // Обмен успешно завершен (для обоих участников)

        // Для аукционов 
        AuctionWon,               // Вы выиграли аукцион
        AuctionOutbid,            // Вашу ставку перебили
        AuctionItemSold,          // Ваш трек продан на аукционе
        AuctionFinishedNoBids,    // Аукцион по вашему треку завершился без ставок

        // Для покупки
        TrackSold,
        TrackPurchased,

        // Другие (например, системные сообщения)
        SystemMessage
    }
}