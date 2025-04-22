using Microsoft.AspNetCore.Mvc;
using SoundTradeWebApp.Models; // Пространство имен для User
using SoundTradeWebApp.Models.ViewModels; // Пространство имен для ViewModels
using SoundTradeWebApp.Data;   // Пространство имен для DbContext
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization; // Для [AllowAnonymous], [Authorize]
using BCrypt.Net; // Для хеширования

namespace SoundTradeWebApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AccountController> _logger;

        public AccountController(ApplicationDbContext context, ILogger<AccountController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // --- РЕГИСТРАЦИЯ ---

        [HttpGet]
        [AllowAnonymous] // Доступно всем
        public IActionResult Register()
        {
            // Если пользователь уже вошел, перенаправляем на главную
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken] // Защита от CSRF
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            // Дополнительная проверка UserType
            if (model.UserType != "Buyer" && model.UserType != "Author")
            {
                ModelState.AddModelError(nameof(model.UserType), "Некорректный тип пользователя.");
            }

            if (ModelState.IsValid)
            {
                // Проверка на существующего пользователя
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

                if (existingUser != null)
                {
                    if (existingUser.Username == model.Username)
                        ModelState.AddModelError(nameof(model.Username), "Пользователь с таким логином уже существует.");
                    if (existingUser.Email == model.Email)
                        ModelState.AddModelError(nameof(model.Email), "Пользователь с таким email уже существует.");

                    return View(model); // Возвращаем форму с ошибками
                }

                // Хешируем пароль (используем BCrypt)
                // GenerateSalt(12) - 12 раундов хеширования (рекомендуется)
                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, BCrypt.Net.BCrypt.GenerateSalt(12));

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    UserType = model.UserType // "Buyer" или "Author"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {user.Username} registered successfully.");

                // Опционально: Автоматический вход после регистрации
                // await SignInUser(user, isPersistent: false);
                // return RedirectToAction("Index", "Home");

                // Перенаправляем на страницу входа с сообщением об успехе (опционально)
                TempData["SuccessMessage"] = "Регистрация прошла успешно! Теперь вы можете войти.";
                return RedirectToAction("Login");
            }

            // Если модель не валидна, возвращаем форму с ошибками
            return View(model);
        }

        // --- ВХОД ---

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToLocal(returnUrl); // Уже вошел, перенаправляем
            }
            ViewData["ReturnUrl"] = returnUrl;
            // Отображаем сообщение об успешной регистрации, если оно есть
            if (TempData["SuccessMessage"] != null)
            {
                ViewBag.SuccessMessage = TempData["SuccessMessage"];
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToLocal(returnUrl);
            }

            ViewData["ReturnUrl"] = returnUrl; // Сохраняем для передачи в View в случае ошибки

            if (ModelState.IsValid)
            {
                var user = await _context.Users
                                 .AsNoTracking() // Чтение без отслеживания для производительности
                                 .FirstOrDefaultAsync(u => u.Username == model.Username);

                // Проверяем пользователя и хеш пароля с использованием BCrypt.Verify
                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // Пароль верный, выполняем вход
                    await SignInUser(user, model.RememberMe);
                    _logger.LogInformation($"User {user.Username} logged in successfully.");
                    return RedirectToLocal(returnUrl); // Перенаправляем
                }
                else
                {
                    // Пользователь не найден или пароль неверный
                    ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
                    _logger.LogWarning($"Failed login attempt for username: {model.Username}");
                }
            }

            // Если модель не валидна или вход не удался, возвращаем форму
            return View(model);
        }

        // --- ВЫХОД ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] // Выйти может только тот, кто вошел
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name ?? "Unknown";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation($"User {userName} logged out.");
            return RedirectToAction("Index", "Home"); // Перенаправляем на главную
        }

        // --- Доступ запрещен ---
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            _logger.LogWarning($"Access denied for user {User.Identity?.Name} to {HttpContext.Request.Path}");
            return View(); // Отображаем представление AccessDenied.cshtml
        }


        // --- Вспомогательные методы ---

        // Метод для выполнения входа пользователя (создание Cookie)
        private async Task SignInUser(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // ID пользователя
                new Claim(ClaimTypes.Name, user.Username),                // Логин пользователя
                new Claim(ClaimTypes.Email, user.Email),                   // Email пользователя
                new Claim(ClaimTypes.Role, user.UserType)                  // Роль пользователя ("Buyer" или "Author")
                // Сюда можно добавить другие клеймы, если они нужны
            };

            // Создаем identity на основе claims и схемы аутентификации
            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            // Настройки аутентификационной куки
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true, // Разрешить обновление куки (если используется SlidingExpiration)

                // Время истечения куки. Устанавливается, если нужно переопределить ExpireTimeSpan из AddCookie
                // ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60),

                // Постоянная куки (переживает закрытие браузера), если RememberMe = true
                IsPersistent = isPersistent,

                // Время выдачи куки (устанавливается автоматически)
                // IssuedUtc = DateTimeOffset.UtcNow,

                // URL для перенаправления после входа (если используется внешний провайдер, здесь не нужно)
                // RedirectUri = <string>
            };

            // Выполняем вход: создаем куки и отправляем ее клиенту
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, // Схема аутентификации
                new ClaimsPrincipal(claimsIdentity),              // Principal с информацией о пользователе
                authProperties);                                  // Свойства куки
        }

        // Метод для безопасного перенаправления на локальный URL
        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                // Если returnUrl локальный и не пустой, перенаправляем на него
                return Redirect(returnUrl);
            }
            else
            {
                // Иначе перенаправляем на главную страницу
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}