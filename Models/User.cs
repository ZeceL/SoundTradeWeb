using System.ComponentModel.DataAnnotations;

namespace SoundTradeWebApp.Models
{
    public class User
    {
        public int Id { get; set; } // Primary Key

        [Required(ErrorMessage = "Требуется имя пользователя")]
        [StringLength(100)]
        public string Username { get; set; } = string.Empty; // Инициализация по умолчанию

        [Required(ErrorMessage = "Требуется Email")]
        [EmailAddress(ErrorMessage = "Некорректный формат Email")]
        [StringLength(255)]
        public string Email { get; set; } = string.Empty; // Инициализация по умолчанию

        [Required]
        public string PasswordHash { get; set; } = string.Empty; // Хеш пароля!

        [Required(ErrorMessage = "Требуется тип пользователя")]
        [StringLength(50)]
        public string UserType { get; set; } = string.Empty; // "Buyer" или "Author"

        // Коллекция треков, загруженных этим пользователем
        public virtual ICollection<Track> Tracks { get; set; } = new List<Track>();
    }
}