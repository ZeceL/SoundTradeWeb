namespace SoundTradeWebApp.Models.Configuration
{
    public class AuctionSettings
    {
        // Задержка перед первой проверкой/запуском аукциона после старта приложения
        public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMinutes(2); // По умолчанию 2 минуты

        // Как часто проверять, не пора ли запустить новый аукцион
        public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(1); // По умолчанию 1 минута

        // Длительность аукциона
        public TimeSpan AuctionDuration { get; set; } = TimeSpan.FromMinutes(5); // По умолчанию 5 минут
    }
}