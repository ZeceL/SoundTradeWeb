namespace SoundTradeWebApp.Models.ViewModels
{
    public class ConfirmPurchaseViewModel
    {
        public int TrackId { get; set; }
        public string? Title { get; set; }
        public string? ArtistName { get; set; } // Имя текущего автора трека
        public string? Genre { get; set; }
        public string? VocalType { get; set; }
        public string? Mood { get; set; }
    }
}