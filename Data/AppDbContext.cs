using LibNode.Api.Models.Enums;
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
    public DbSet<UserCollection> UserCollections => Set<UserCollection>();
    public DbSet<CollectionBook> CollectionBooks => Set<CollectionBook>();
    public DbSet<ChapterLike> ChapterLikes => Set<ChapterLike>();
    public DbSet<ReadingProgress> ReadingProgresses => Set<ReadingProgress>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Category> Categories => Set<Category>();

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

            entity.Property(b => b.Type)
                  .HasConversion<int>()
                  .HasDefaultValue(BookType.Japan);

            entity.Property(b => b.OriginalStatus)
                  .HasConversion<int>()
                  .HasDefaultValue(OriginalStatus.None);

            entity.Property(b => b.TranslationStatus)
                  .HasConversion<int>()
                  .HasDefaultValue(TranslationStatus.None);

            entity.Property(b => b.CreatedAt)
                  .HasDefaultValueSql("now()");

            entity.Property(b => b.UpdatedAt)
                  .HasDefaultValueSql("now()");

            entity.HasIndex(b => b.Title);

            entity.HasMany(b => b.Tags)
                  .WithMany(t => t.Books);

            entity.HasMany(b => b.Categories)
                  .WithMany(c => c.Books);
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

        // ── Tag ──────────────────────────────────────────
        modelBuilder.Entity<Tag>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(t => t.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(t => t.Slug)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(t => t.CreatedAt)
                  .HasDefaultValueSql("now()");

            entity.Property(t => t.UpdatedAt)
                  .HasDefaultValueSql("now()");

            entity.HasIndex(t => t.Slug).IsUnique();
        });

        // ── Category ─────────────────────────────────────
        modelBuilder.Entity<Category>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(c => c.Name)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(c => c.Slug)
                  .IsRequired()
                  .HasMaxLength(100);

            entity.Property(c => c.CreatedAt)
                  .HasDefaultValueSql("now()");

            entity.Property(c => c.UpdatedAt)
                  .HasDefaultValueSql("now()");

            entity.HasIndex(c => c.Slug).IsUnique();
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

        // ── UserCollection ──────────────────────────────
        modelBuilder.Entity<UserCollection>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(c => c.Name)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(c => c.CreatedAt)
                  .HasDefaultValueSql("now()");

            // FK: UserCollection.UserId → User.Id (CASCADE DELETE)
            entity.HasOne(c => c.User)
                  .WithMany(u => u.Collections)
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── CollectionBook ──────────────────────────────
        modelBuilder.Entity<CollectionBook>(entity =>
        {
            // Составной первичный ключ
            entity.HasKey(cb => new { cb.CollectionId, cb.BookId });

            entity.Property(cb => cb.AddedAt)
                  .HasDefaultValueSql("now()");

            // FK: CollectionBook.CollectionId → UserCollection.Id
            entity.HasOne(cb => cb.Collection)
                  .WithMany(c => c.CollectionBooks)
                  .HasForeignKey(cb => cb.CollectionId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK: CollectionBook.BookId → Book.Id
            entity.HasOne(cb => cb.Book)
                  .WithMany(b => b.CollectionBooks)
                  .HasForeignKey(cb => cb.BookId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── ChapterLike ─────────────────────────────────────────
        modelBuilder.Entity<ChapterLike>(entity =>
        {
            // Составной первичный ключ
            entity.HasKey(cl => new { cl.UserId, cl.ChapterId });

            entity.Property(cl => cl.CreatedAt)
                  .HasDefaultValueSql("now()");

            // FK: ChapterLike.UserId → User.Id
            entity.HasOne(cl => cl.User)
                  .WithMany(u => u.ChapterLikes)
                  .HasForeignKey(cl => cl.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK: ChapterLike.ChapterId → Chapter.Id
            entity.HasOne(cl => cl.Chapter)
                  .WithMany(c => c.Likes)
                  .HasForeignKey(cl => cl.ChapterId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ReadingProgress>(entity =>
        {
            entity.HasKey(rp => new { rp.UserId, rp.BookId });

            entity.Property(rp => rp.UpdatedAt)
                  .HasDefaultValueSql("now()");

            entity.HasOne(rp => rp.User)
                  .WithMany(u => u.ReadingProgresses)
                  .HasForeignKey(rp => rp.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Book)
                  .WithMany(b => b.ReadingProgresses)
                  .HasForeignKey(rp => rp.BookId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(rp => rp.Chapter)
                  .WithMany(c => c.ReadingProgresses)
                  .HasForeignKey(rp => rp.ChapterId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    public override int SaveChanges()
    {
        AddAuditInfo();
        return base.SaveChanges();
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        AddAuditInfo();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        AddAuditInfo();
        return base.SaveChangesAsync(cancellationToken);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        AddAuditInfo();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void AddAuditInfo()
    {
        var entries = ChangeTracker.Entries().Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);
        var now = DateTime.UtcNow;

        foreach (var entry in entries)
        {
            if (entry.State == EntityState.Added)
            {
                // Генерация UUIDv7 для новых сущностей с пустым Guid-ключом
                var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "Id");
                if (idProp != null && idProp.CurrentValue is Guid guidValue && guidValue == Guid.Empty)
                {
                    idProp.CurrentValue = Guid.CreateVersion7();
                }

                var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProp != null)
                {
                    createdAtProp.CurrentValue = now;
                }

                var addedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "AddedAt");
                if (addedAtProp != null)
                {
                    addedAtProp.CurrentValue = now;
                }

                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null)
                {
                    updatedAtProp.CurrentValue = now;
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null)
                {
                    updatedAtProp.CurrentValue = now;
                }
            }
        }
    }
}
