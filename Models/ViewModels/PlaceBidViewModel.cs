using System.ComponentModel.DataAnnotations;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class PlaceBidViewModel
    {
        [Required]
        public int AuctionId { get; set; } // ID аукциона, на который делается ставка

        [Required(ErrorMessage = "Введите сумму ставки")]
        [DataType(DataType.Currency)]
        [Range(1.00, double.MaxValue, ErrorMessage = "Ставка должна быть положительной")] // Простая проверка > 0
        [Display(Name = "Ваша ставка")]
        public decimal BidAmount { get; set; } // Сумма, предложенная пользователем
    }
}