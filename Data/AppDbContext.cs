using LibNode.Api.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace LibNode.Api.Data;

/// <summary>
/// Контекст базы данных приложения. Содержит Fluent API конфигурацию.
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Book> Books => Set<Book>();
    public DbSet<Chapter> Chapters => Set<Chapter>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Book ────────────────────────────────────────
        modelBuilder.Entity<Book>(entity =>
        {
            entity.HasKey(b => b.Id);

            entity.Property(b => b.Id)
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(b => b.Title)
                  .IsRequired()
                  .HasMaxLength(300);

            entity.Property(b => b.Description)
                  .HasMaxLength(5000);

            entity.Property(b => b.CoverUrl)
                  .HasMaxLength(2048);

            entity.Property(b => b.CreatedAt)
                  .HasDefaultValueSql("now()");

            entity.Property(b => b.UpdatedAt)
                  .HasDefaultValueSql("now()");

            // Индекс для поиска и сортировки по названию
            entity.HasIndex(b => b.Title);
        });

        // ── Chapter ─────────────────────────────────────
        modelBuilder.Entity<Chapter>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(c => c.Title)
                  .IsRequired()
                  .HasMaxLength(500);

            entity.Property(c => c.Content)
                  .IsRequired();

            entity.Property(c => c.CreatedAt)
                  .HasDefaultValueSql("now()");

            // FK: Chapter.BookId → Book.Id (CASCADE DELETE)
            entity.HasOne(c => c.Book)
                  .WithMany(b => b.Chapters)
                  .HasForeignKey(c => c.BookId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Составной индекс для быстрой выборки глав книги по порядку
            entity.HasIndex(c => new { c.BookId, c.ChapterNumber })
                  .IsUnique();
        });

        // ── User ────────────────────────────────────────
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(u => u.Id);

            entity.Property(u => u.Id)
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(u => u.Username)
                  .IsRequired()
                  .HasMaxLength(50);

            entity.Property(u => u.Email)
                  .IsRequired()
                  .HasMaxLength(256);

            entity.Property(u => u.PasswordHash)
                  .IsRequired();

            entity.Property(u => u.Role)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasDefaultValue("User");

            entity.Property(u => u.CreatedAt)
                  .HasDefaultValueSql("now()");

            // Уникальные индексы
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.Username).IsUnique();
        });
    }
}
