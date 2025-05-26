using SoundTradeWebApp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoundTradeWebApp.Models
{
    public class ExchangeOffer
    {
        [Key]
        public int Id { get; set; }

        // Инициатор обмена
        [Required]
        public int InitiatorUserId { get; set; }
        [ForeignKey("InitiatorUserId")]
        public virtual User? Initiator { get; set; }

        // Трек, предложенный инициатором
        [Required]
        public int OfferedTrackId { get; set; }
        [ForeignKey("OfferedTrackId")]
        public virtual Track? OfferedTrack { get; set; }

        // Получатель предложения
        [Required]
        public int RecipientUserId { get; set; }
        [ForeignKey("RecipientUserId")]
        public virtual User? Recipient { get; set; }

        // Трек, запрошенный у получателя
        [Required]
        public int RequestedTrackId { get; set; }
        [ForeignKey("RequestedTrackId")]
        public virtual Track? RequestedTrack { get; set; }

        [Required]
        public ExchangeStatus Status { get; set; } = ExchangeStatus.Pending;

        public DateTime OfferDate { get; set; } = DateTime.UtcNow;
        public DateTime? ResponseDate { get; set; } // Дата ответа (принятия/отклонения)
    }
}