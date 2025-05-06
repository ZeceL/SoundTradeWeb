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

        public DbSet<AuctionSubmission> AuctionSubmissions { get; set; }

        public DbSet<Auction> Auctions { get; set; }

        public DbSet<Bid> Bids { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder); // Важно вызвать базовый метод

            // --- Конфигурация для точности decimal ---
            modelBuilder.Entity<AuctionSubmission>().Property(s => s.StartingBid).HasPrecision(18, 2);
            modelBuilder.Entity<Auction>().Property(a => a.StartingBid).HasPrecision(18, 2);
            modelBuilder.Entity<Auction>().Property(a => a.CurrentHighestBid).HasPrecision(18, 2);
            modelBuilder.Entity<Bid>().Property(b => b.BidAmount).HasPrecision(18, 2);

            // --- Настройка каскадного удаления для предотвращения циклов ---

            // Связь User -> AuctionSubmission (Автор заявки)
            modelBuilder.Entity<AuctionSubmission>()
                .HasOne(s => s.OriginalAuthor) // У заявки один автор
                .WithMany() // У пользователя может быть много заявок (не указываем обратное свойство в User)
                .HasForeignKey(s => s.OriginalAuthorUserId)
                .OnDelete(DeleteBehavior.Restrict); // <<--- ИЗМЕНЕНО НА RESTRICT

            // Связь User -> Track (Автор трека)
            modelBuilder.Entity<Track>()
                .HasOne(t => t.AuthorUser) // У трека один автор
                .WithMany(u => u.Tracks) // Обратное свойство в User
                .HasForeignKey(t => t.AuthorUserId)
                .OnDelete(DeleteBehavior.Restrict); // <<--- ИЗМЕНЕНО НА RESTRICT

            // Связь User -> Bid (Автор ставки)
            modelBuilder.Entity<Bid>()
                .HasOne(b => b.Bidder) // У ставки один автор
                .WithMany() // У пользователя много ставок
                .HasForeignKey(b => b.BidderUserId)
                .OnDelete(DeleteBehavior.Restrict); // <<--- ИЗМЕНЕНО НА RESTRICT

            // Связи User -> Auction (Лидер/Победитель)
            // Так как внешние ключи CurrentHighestBidderUserId и WinnerUserId являются Nullable (int?),
            // EF Core по умолчанию для них обычно ставит DeleteBehavior.ClientSetNull или Restrict,
            // что не должно вызывать циклов. Оставим поведение по умолчанию для них.
            // Если бы возникла ошибка и с ними, можно было бы настроить явно:
            // .OnDelete(DeleteBehavior.SetNull);

            // --- Оставляем Cascade для связей от Track и Auction ---
            // (Предполагаем, что при удалении трека нужно удалить связанные заявки/аукционы,
            // а при удалении аукциона - связанные ставки)

            // Связь Track -> AuctionSubmission
            modelBuilder.Entity<AuctionSubmission>()
                .HasOne(s => s.Track)
                .WithMany() // У трека может быть несколько заявок
                .HasForeignKey(s => s.TrackId)
                .OnDelete(DeleteBehavior.Cascade); // Удалить заявки при удалении трека

            // Связь Track -> Auction
            modelBuilder.Entity<Auction>()
                .HasOne(a => a.Track)
                .WithMany() // У трека может быть несколько аукционов (в истории)
                .HasForeignKey(a => a.TrackId)
                .OnDelete(DeleteBehavior.Cascade); // Удалить аукцион при удалении трека

            // Связь Auction -> Bid
            modelBuilder.Entity<Bid>()
                 .HasOne(b => b.Auction)
                 .WithMany(a => a.Bids) // Используем обратное свойство Bids в Auction
                 .HasForeignKey(b => b.AuctionId)
                 .OnDelete(DeleteBehavior.Cascade); // Удалить ставки при удалении аукциона


            // --- Ваши существующие конфигурации индексов для User ---
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }
    }
}