using SoundTradeWebApp.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class AuctionDetailsViewModel
    {
        public int AuctionId { get; set; }
        public int TrackId { get; set; }
        public string TrackTitle { get; set; } = string.Empty;
        public string ArtistName { get; set; } = string.Empty;
        public string AudioUrl { get; set; } = string.Empty; // URL для кнопки "Слушать"

        [DisplayFormat(DataFormatString = "{0:N2} руб.")]
        public decimal StartingBid { get; set; }

        [DisplayFormat(DataFormatString = "{0:N2} руб.")]
        public decimal? CurrentBid { get; set; } // Текущая высшая ставка

        public string HighestBidderUsername { get; set; } = "Ставок еще нет"; // Имя лидера

        [Display(Name = "Следующая минимальная ставка")]
        [DisplayFormat(DataFormatString = "{0:N2} руб.")]
        public decimal NextMinimumBid { get; set; } // Рассчитывается (CurrentBid ?? StartingBid) + Increment

        public int BidIncrement { get; set; }

        [Display(Name = "Осталось времени")]
        public TimeSpan TimeRemaining { get; set; } // Оставшееся время
        public DateTime EndTime { get; set; } // Точное время окончания для JS таймера
        public AuctionStatus Status { get; set; }

        // Список последних ставок (опционально)
        // public List<BidViewModel> RecentBids { get; set; } = new();

        // Для формы новой ставки
        [Required(ErrorMessage = "Введите сумму ставки")]
        [Display(Name = "Ваша ставка (руб.)")]
        // Можно добавить валидацию Range, которая зависит от NextMinimumBid (сложнее)
        public decimal? NewBidAmount { get; set; } // Поле ввода для новой ставки
    }
}