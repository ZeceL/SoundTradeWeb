using Microsoft.EntityFrameworkCore;
using SoundTradeWebApp.Models; // Пространство имен ваших моделей

namespace SoundTradeWebApp.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // DbSet представляет таблицу Users в базе данных
        public DbSet<User> Users { get; set; }

        public DbSet<Track> Tracks { get; set; }

        // Здесь можно добавить DbSet для других сущностей (песни, тексты и т.д.)

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Добавление уникальных индексов для полей Username и Email
            // Гарантирует, что не будет двух пользователей с одинаковым логином или email
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            // Здесь можно добавить другие конфигурации Fluent API
        }
    }
}