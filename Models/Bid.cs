using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoundTradeWebApp.Models
{
    public class Bid
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int AuctionId { get; set; } // Внешний ключ к аукциону

        [Required]
        public int BidderUserId { get; set; } // Внешний ключ к пользователю, сделавшему ставку

        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal BidAmount { get; set; } // Сумма ставки

        [Required]
        public DateTime BidTime { get; set; } = DateTime.UtcNow; // Время ставки

        // Навигационные свойства
        [ForeignKey("AuctionId")]
        public virtual Auction? Auction { get; set; }

        [ForeignKey("BidderUserId")]
        public virtual User? Bidder { get; set; }
    }
}
