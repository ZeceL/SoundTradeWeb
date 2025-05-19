using Microsoft.AspNetCore.Mvc;
using SoundTradeWebApp.Models; 
using SoundTradeWebApp.Models.ViewModels; 
using SoundTradeWebApp.Data;   
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization; 
using BCrypt.Net; 

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

        [HttpGet]
        [AllowAnonymous] 
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken] 
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }

            if (model.UserType != "Buyer" && model.UserType != "Author")
            {
                ModelState.AddModelError(nameof(model.UserType), "Некорректный тип пользователя.");
            }

            if (ModelState.IsValid)
            {
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username || u.Email == model.Email);

                if (existingUser != null)
                {
                    if (existingUser.Username == model.Username)
                        ModelState.AddModelError(nameof(model.Username), "Пользователь с таким логином уже существует.");
                    if (existingUser.Email == model.Email)
                        ModelState.AddModelError(nameof(model.Email), "Пользователь с таким email уже существует.");

                    return View(model); 
                }

                string passwordHash = BCrypt.Net.BCrypt.HashPassword(model.Password, BCrypt.Net.BCrypt.GenerateSalt(12));

                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = passwordHash,
                    UserType = model.UserType 
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {user.Username} registered successfully.");

                TempData["SuccessMessage"] = "Регистрация прошла успешно! Теперь вы можете войти.";
                return RedirectToAction("Login");
            }

            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToLocal(returnUrl); 
            }
            ViewData["ReturnUrl"] = returnUrl;
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

            ViewData["ReturnUrl"] = returnUrl; 

            if (ModelState.IsValid)
            {
                var user = await _context.Users
                                 .AsNoTracking() 
                                 .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    await SignInUser(user, model.RememberMe);
                    _logger.LogInformation($"User {user.Username} logged in successfully.");
                    return RedirectToLocal(returnUrl);
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Неверный логин или пароль.");
                    _logger.LogWarning($"Failed login attempt for username: {model.Username}");
                }
            }
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize] 
        public async Task<IActionResult> Logout()
        {
            var userName = User.Identity?.Name ?? "Unknown";
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            _logger.LogInformation($"User {userName} logged out.");
            return RedirectToAction("Index", "Home"); 
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            _logger.LogWarning($"Access denied for user {User.Identity?.Name} to {HttpContext.Request.Path}");
            return View(); 
        }

        private async Task SignInUser(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                new Claim(ClaimTypes.Name, user.Username),                
                new Claim(ClaimTypes.Email, user.Email),                   
                new Claim(ClaimTypes.Role, user.UserType)
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true, 

                IsPersistent = isPersistent,

            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme, 
                new ClaimsPrincipal(claimsIdentity),              
                authProperties);                                  
        }

        private IActionResult RedirectToLocal(string? returnUrl)
        {
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }
    }
}