using SoundTradeWebApp.Enums;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoundTradeWebApp.Models
{
    public class Notification
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int RecipientUserId { get; set; } // Кому адресовано уведомление
        [ForeignKey("RecipientUserId")]
        public virtual User? Recipient { get; set; }

        [Required]
        [StringLength(500)]
        public string Message { get; set; } = string.Empty; // Текст уведомления

        [Required]
        public NotificationType Type { get; set; } // Тип уведомления

        public bool IsRead { get; set; } = false; // Прочитано ли
        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

        // Опциональные поля для связи с конкретными сущностями
        public int? RelatedItemId { get; set; } // Например, ID ExchangeOffer или Auction
        public string? LinkUrl { get; set; } // Прямая ссылка на связанный элемент (например, на страницу предложения обмена)
    }
}