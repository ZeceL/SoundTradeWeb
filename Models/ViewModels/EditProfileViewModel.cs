using System.ComponentModel.DataAnnotations;

namespace SoundTradeWebApp.Models.ViewModels
{
    public class EditProfileViewModel
    {
        [Required(ErrorMessage = "Логин обязателен")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Логин должен быть от 3 до 100 символов")]
        [Display(Name = "Логин")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email обязателен")]
        [EmailAddress(ErrorMessage = "Неверный формат Email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = string.Empty;

        // Поля для смены пароля (не обязательные)
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Новый пароль должен быть не менее 6 символов")]
        [Display(Name = "Новый пароль (оставьте пустым, если не меняете)")]
        public string? NewPassword { get; set; } // Nullable, так как смена не обязательна

        [DataType(DataType.Password)]
        [Compare("NewPassword", ErrorMessage = "Пароли не совпадают")]
        [Display(Name = "Подтвердите новый пароль")]
        public string? ConfirmPassword { get; set; }
    }
}