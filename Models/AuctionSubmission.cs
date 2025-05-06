using SoundTradeWebApp.Enums; // Подключаем наш enum
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoundTradeWebApp.Models
{
    public class AuctionSubmission
    {
        [Key] // Первичный ключ
        public int Id { get; set; }

        [Required]
        public int TrackId { get; set; } // Внешний ключ к треку

        [Required]
        public int OriginalAuthorUserId { get; set; } // Внешний ключ к пользователю-автору

        [Required]
        [Column(TypeName = "decimal(18, 2)")] // Точность для денег
        [Range(0, (double)decimal.MaxValue, ErrorMessage = "Начальная ставка не может быть отрицательной.")]
        [Display(Name = "Начальная ставка (руб.)")]
        public decimal StartingBid { get; set; }

        [Required(ErrorMessage = "Необходимо выбрать шаг ставки")]
        [Display(Name = "Шаг ставки (руб.)")]
        public int BidIncrement { get; set; } // 500, 1000 или 2000

        [Required]
        public DateTime SubmissionTime { get; set; } = DateTime.UtcNow; // Время подачи заявки

        [Required]
        public AuctionStatus Status { get; set; } = AuctionStatus.Pending; // Статус заявки

        // Навигационные свойства (для удобной работы с связанными данными)
        [ForeignKey("TrackId")]
        public virtual Track? Track { get; set; }

        [ForeignKey("OriginalAuthorUserId")]
        public virtual User? OriginalAuthor { get; set; }
    }
}