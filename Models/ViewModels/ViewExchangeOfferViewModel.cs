using SoundTradeWebApp.Enums;
using System;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class ViewExchangeOfferViewModel
    {
        public int OfferId { get; set; }

        // Информация о треке, который ПРЕДЛАГАЕТ инициатор
        public int OfferedTrackId { get; set; }
        public string? OfferedTrackTitle { get; set; }
        public string? OfferedTrackArtistName { get; set; } // Имя инициатора на момент предложения
        public string? OfferedTrackAudioUrl { get; set; }

        // Информация о треке, который ЗАПРАШИВАЮТ у текущего пользователя (получателя)
        public int RequestedTrackId { get; set; }
        public string? RequestedTrackTitle { get; set; }
        public string? RequestedTrackArtistName { get; set; } // Имя получателя на момент предложения
        public string? RequestedTrackAudioUrl { get; set; }

        // Информация об инициаторе
        public int InitiatorUserId { get; set; }
        public string? InitiatorUsername { get; set; }

        public int RecipientUserId { get; set; }

        public ExchangeStatus Status { get; set; }
        public DateTime OfferDate { get; set; }
        public DateTime? ResponseDate { get; set; }

        // Для кнопок действия (если статус Pending)
        public bool CanRespond { get; set; } // true, если статус Pending и текущий юзер - получатель
    }
}