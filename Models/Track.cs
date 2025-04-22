using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoundTradeWebApp.Models
{
    public class Track
    {
        public int Id { get; set; } // Primary Key

        [Required(ErrorMessage = "Требуется название трека")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Требуется имя исполнителя/автора")]
        [StringLength(150)]
        public string ArtistName { get; set; } = string.Empty;

        [StringLength(100)]
        public string? Genre { get; set; }

        [StringLength(50)]
        public string? VocalType { get; set; }

        [StringLength(50)]
        public string? Mood { get; set; }

        [DataType(DataType.MultilineText)]
        public string? Lyrics { get; set; }

        [Required(ErrorMessage = "Требуется содержимое аудиофайла")]
        public byte[] AudioFileContent { get; set; } = Array.Empty<byte>(); // Массив байт для файла

        [Required(ErrorMessage = "Требуется тип контента")]
        [StringLength(100)]
        public string AudioContentType { get; set; } = string.Empty; // MIME-тип файла

        [Required]
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        // Связь с пользователем (Автором)
        [Required]
        public int AuthorUserId { get; set; } // Foreign Key

        [ForeignKey("AuthorUserId")]
        public virtual User? AuthorUser { get; set; } // Навигационное свойство
    }
}