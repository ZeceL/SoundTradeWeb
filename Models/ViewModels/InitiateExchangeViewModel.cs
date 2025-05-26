using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class InitiateExchangeViewModel
    {
        // Информация о треке, который пользователь ХОЧЕТ ПОЛУЧИТЬ
        [Required]
        public int RequestedTrackId { get; set; }
        public string? RequestedTrackTitle { get; set; }
        public string? RequestedTrackArtistName { get; set; }
        public int RequestedTrackOwnerId { get; set; } // ID владельца запрашиваемого трека

        // Трек, который текущий пользователь ПРЕДЛАГАЕТ ВЗАМЕН
        [Required(ErrorMessage = "Необходимо выбрать трек, который вы предлагаете для обмена.")]
        [Display(Name = "Ваш трек для обмена")]
        public int OfferedTrackId { get; set; }

        // Список треков текущего пользователя, доступных для предложения
        public List<SelectListItem> AvailableTracksToOffer { get; set; } = new List<SelectListItem>();
    }
}