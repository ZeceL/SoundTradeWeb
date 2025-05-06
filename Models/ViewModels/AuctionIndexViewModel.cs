using System.Collections.Generic;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class AuctionIndexViewModel
    {
        public bool IsAuctionActive { get; set; } = false; // Флаг: идет ли сейчас аукцион?
        public bool CanSubmitTrack { get; set; } = false; // Флаг: может ли текущий пользователь подать заявку?

        // Информация о текущем активном аукционе (заполняется, если IsAuctionActive = true)
        // TODO: Создать ViewModel для деталей аукциона (AuctionDetailsViewModel)
        public AuctionDetailsViewModel? CurrentAuction { get; set; }

        // Модель для формы подачи новой заявки (заполняется, если CanSubmitTrack = true)
        public AuctionSubmissionViewModel? NewSubmission { get; set; }
    }
}