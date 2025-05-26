namespace SoundTradeWebApp.Enums
{
    public enum ExchangeStatus
    {
        Pending,    // 0 - Ожидает ответа от получателя
        Accepted,   // 1 - Принято получателем
        Declined,   // 2 - Отклонено получателем
        Cancelled   // 3 - Отменено инициатором 
    }
}
