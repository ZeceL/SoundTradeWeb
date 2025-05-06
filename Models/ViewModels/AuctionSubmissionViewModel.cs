using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class AuctionSubmissionViewModel
    {
        [Required(ErrorMessage = "Необходимо выбрать трек")]
        [Range(1, int.MaxValue, ErrorMessage = "Необходимо выбрать трек")] // Ловит 0
        [Display(Name = "Трек для аукциона")]
        public int TrackId { get; set; }

        [Required(ErrorMessage = "Укажите начальную ставку")]
        [Range(1.00, 1000000.00, ErrorMessage = "Ставка должна быть от 1.00 до 1,000,000.00")] // Ловит 0
        [DataType(DataType.Currency)]
        [Display(Name = "Начальная ставка (руб.)")]
        public decimal StartingBid { get; set; }

        [Required(ErrorMessage = "Выберите шаг ставки")]
        [Range(500, 2000, ErrorMessage = "Шаг ставки должен быть 500, 1000 или 2000")] // Ловит 0 и неверные значения
        [Display(Name = "Шаг ставки (руб.)")]
        public int BidIncrement { get; set; }

        // Списки для dropdowns
        public List<SelectListItem> AvailableTracks { get; set; } = new();
        public List<SelectListItem> AvailableIncrements { get; set; } = new();
    }
}