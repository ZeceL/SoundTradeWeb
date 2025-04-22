using Microsoft.AspNetCore.Mvc;
using SoundTradeWebApp.Models; // Для ErrorViewModel
using System.Diagnostics;

namespace SoundTradeWebApp.Controllers
{
    public class HomeController : Controller
    {
        // Добавим конструктор для логгера, если нужно будет логировать
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        // Главная страница
        public IActionResult Index()
        {
            // Здесь можно будет передавать данные на главную страницу, если нужно
            return View();
        }

        // Страница ошибки (используется UseExceptionHandler в Program.cs)
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            var errorViewModel = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
            // Можно добавить логирование ошибки здесь
            _logger.LogError($"Error Occurred. TraceId: {errorViewModel.RequestId}");
            return View(errorViewModel);
        }

    }
}