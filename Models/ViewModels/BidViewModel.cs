using System;
using System.ComponentModel.DataAnnotations;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class BidViewModel
    {
        public string BidderUsername { get; set; } = "Аноним"; // Имя сделавшего ставку
        [DisplayFormat(DataFormatString = "{0:N2} руб.")] // Формат валюты
        public decimal BidAmount { get; set; }
        public DateTime BidTime { get; set; } // Время ставки
        public string TimeAgo { get; set; } = string.Empty; // Текстовое представление (напр. "5 сек назад") - опционально
    }
}