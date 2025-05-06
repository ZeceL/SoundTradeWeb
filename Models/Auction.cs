using SoundTradeWebApp.Enums;
using System;
using System.Collections.Generic; // Для коллекции ставок
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Security.Cryptography;

namespace SoundTradeWebApp.Models
{
    public class Auction
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TrackId { get; set; } // Внешний ключ к треку

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; } // Рассчитывается при создании (StartTime + 5 мин)

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal StartingBid { get; set; } // Начальная ставка

        // Текущая высшая ставка (может быть null, если ставок еще нет)
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? CurrentHighestBid { get; set; }

        // ID пользователя с высшей ставкой (может быть null)
        public int? CurrentHighestBidderUserId { get; set; } // Nullable внешний ключ

        [Required]
        public int BidIncrement { get; set; } // Шаг ставки

        [Required]
        public AuctionStatus Status { get; set; } = AuctionStatus.Scheduled; // Изначально запланирован или сразу активен

        // ID победителя (после завершения)
        public int? WinnerUserId { get; set; } // Nullable внешний ключ

        // Навигационные свойства
        [ForeignKey("TrackId")]
        public virtual Track? Track { get; set; }

        [ForeignKey("CurrentHighestBidderUserId")]
        public virtual User? CurrentHighestBidder { get; set; }

        [ForeignKey("WinnerUserId")]
        public virtual User? Winner { get; set; }

        // Коллекция всех ставок, сделанных на этом аукционе
        public virtual ICollection<Bid> Bids { get; set; } = new List<Bid>();
    }
}