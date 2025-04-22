using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Data; // Ваше пространство имен для DbContext

var builder = WebApplication.CreateBuilder(args);

// --- Регистрация сервисов ---

// 1. Добавление поддержки MVC (контроллеры и представления)
builder.Services.AddControllersWithViews();

// 2. Регистрация DbContext для работы с SQL Server
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// 3. Настройка Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";       // Страница входа (если доступ запрещен)
        options.AccessDeniedPath = "/Account/AccessDenied"; // Страница ошибки доступа (если роль не подходит)
        options.ExpireTimeSpan = TimeSpan.FromDays(30); // Время жизни куки (30 дней)
        options.SlidingExpiration = true;           // Продлевать время жизни при активности
        options.Cookie.HttpOnly = true;             // Куки доступны только серверу (защита от XSS)
        options.Cookie.IsEssential = true;          // Помечаем куки как необходимые (для GDPR)
        // options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Только HTTPS (раскомментировать при развертывании на HTTPS)
    });

// 4. Добавление сервисов авторизации
builder.Services.AddAuthorization();

// --- Сборка приложения ---
var app = builder.Build();

// --- Настройка Pipeline (Middleware) ---

// 5. Обработка ошибок (в режиме разработки показывать детальную страницу)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Страница для отображения ошибок пользователю
    app.UseHsts(); // Принудительное использование HTTPS (после первого успешного соединения)
}
else
{
    app.UseDeveloperExceptionPage(); // Страница с деталями ошибки для разработчика
}

// 6. Перенаправление на HTTPS
app.UseHttpsRedirection();

// 7. Поддержка статических файлов (CSS, JS, изображения из wwwroot)
app.UseStaticFiles();

// 8. Маршрутизация запросов
app.UseRouting();

// 9. Включение аутентификации (ВАЖНО: перед UseAuthorization)
app.UseAuthentication();

// 10. Включение авторизации
app.UseAuthorization();

// 11. Настройка маршрутов по умолчанию (Controller=Home, Action=Index, опциональный id)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// --- Запуск приложения ---
app.Run();