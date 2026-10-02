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
    public DbSet<Quote> Quotes => Set<Quote>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<CommentLike> CommentLikes => Set<CommentLike>();
    public DbSet<ChapterRead> ChapterReads => Set<ChapterRead>();
    public DbSet<Team> Teams => Set<Team>();
    public DbSet<TeamMember> TeamMembers => Set<TeamMember>();
    public DbSet<TeamTitleRequest> TeamTitleRequests => Set<TeamTitleRequest>();
    public DbSet<TeamInvite> TeamInvites => Set<TeamInvite>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<CommentReport> CommentReports => Set<CommentReport>();
    public DbSet<ChapterVersion> ChapterVersions => Set<ChapterVersion>();
    public DbSet<ChapterVersionVote> ChapterVersionVotes => Set<ChapterVersionVote>();
    public DbSet<UserStats> UserStats => Set<UserStats>();
    public DbSet<UserAchievement> UserAchievements => Set<UserAchievement>();
    public DbSet<UserQuest> UserQuests => Set<UserQuest>();
    public DbSet<Follow> Follows => Set<Follow>();
    public DbSet<BookRating> BookRatings => Set<BookRating>();
    public DbSet<BookShelf> BookShelves => Set<BookShelf>();
    public DbSet<UserNotificationPrefs> UserNotificationPrefs => Set<UserNotificationPrefs>();

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

            entity.Property(b => b.Slug)
                  .HasMaxLength(300);

            entity.Property(b => b.Description)
                  .HasMaxLength(5000);

            entity.Property(b => b.CoverUrl)
                  .HasMaxLength(2048);

            entity.Property(b => b.CoverThumbUrl)
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

            entity.Property(b => b.ViewCount)
                  .HasDefaultValue(0L);

            entity.Property(b => b.CreatedAt)
                  .HasDefaultValueSql("now()");

            entity.Property(b => b.UpdatedAt)
                  .HasDefaultValueSql("now()");

            entity.HasIndex(b => b.Title);
            entity.HasIndex(b => b.Slug).IsUnique();
            // Для ленты подписок и фильтра каталога по команде.
            entity.HasIndex(b => b.TeamId);

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

            entity.Property(c => c.IsPublished)
                  .HasDefaultValue(true);

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

            entity.Property(u => u.SecurityStamp)
                  .IsRequired()
                  .HasMaxLength(64);

            entity.Property(u => u.Role)
                  .IsRequired()
                  .HasMaxLength(20)
                  .HasDefaultValue("User");

            entity.Property(u => u.AvatarUrl)
                  .HasMaxLength(500);

            entity.Property(u => u.AvatarThumbUrl)
                  .HasMaxLength(500);

            entity.Property(u => u.Bio)
                  .HasMaxLength(1000);

            entity.Property(u => u.IsBanned).HasDefaultValue(false);
            entity.Property(u => u.IsMuted).HasDefaultValue(false);

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

        // ── Quote ───────────────────────────────────────────
        modelBuilder.Entity<Quote>(entity =>
        {
            entity.HasKey(q => q.Id);

            entity.Property(q => q.Id)
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(q => q.SelectedText)
                  .IsRequired();

            entity.Property(q => q.ContextText)
                  .HasMaxLength(5000);

            entity.Property(q => q.Note)
                  .HasMaxLength(2000);

            entity.Property(q => q.CreatedAt)
                  .HasDefaultValueSql("now()");

            entity.Property(q => q.UpdatedAt)
                  .HasDefaultValueSql("now()");

            // FK: Quote.UserId → User.Id (CASCADE DELETE)
            entity.HasOne(q => q.User)
                  .WithMany()
                  .HasForeignKey(q => q.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK: Quote.ChapterId → Chapter.Id (CASCADE DELETE)
            entity.HasOne(q => q.Chapter)
                  .WithMany()
                  .HasForeignKey(q => q.ChapterId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK: Quote.BookId → Book.Id (CASCADE DELETE)
            entity.HasOne(q => q.Book)
                  .WithMany()
                  .HasForeignKey(q => q.BookId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Быстрая выборка цитат пользователя и цитат по книге
            entity.HasIndex(q => new { q.UserId, q.CreatedAt });
            entity.HasIndex(q => new { q.BookId, q.UserId });
        });

        // ── Comment ─────────────────────────────────────────
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Id)
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(c => c.Content)
                  .IsRequired()
                  .HasMaxLength(2000);

            entity.Property(c => c.IsPinned)
                  .HasDefaultValue(false);

            entity.Property(c => c.CreatedAt)
                  .HasDefaultValueSql("now()");

            entity.Property(c => c.UpdatedAt)
                  .HasDefaultValueSql("now()");

            // FK: Comment.BookId → Book.Id (CASCADE DELETE)
            entity.HasOne(c => c.Book)
                  .WithMany()
                  .HasForeignKey(c => c.BookId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Самоссылка: Comment.ParentId → Comment.Id (ответы; CASCADE DELETE)
            entity.HasOne(c => c.Parent)
                  .WithMany(c => c.Replies)
                  .HasForeignKey(c => c.ParentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(c => c.ParentId);

            // FK: Comment.ChapterId → Chapter.Id (CASCADE DELETE, nullable)
            entity.HasOne(c => c.Chapter)
                  .WithMany()
                  .HasForeignKey(c => c.ChapterId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK: Comment.UserId → User.Id (CASCADE DELETE)
            entity.HasOne(c => c.User)
                  .WithMany()
                  .HasForeignKey(c => c.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Списки комментариев книги (ChapterId == null) и главы, новейшие первыми по Id (UUIDv7).
            entity.HasIndex(c => new { c.BookId, c.ChapterId, c.Id });
            entity.HasIndex(c => new { c.ChapterId, c.Id });
            // Для лидерборда (топ комментаторов) и выборки комментариев пользователя.
            entity.HasIndex(c => c.UserId);
        });

        // ── CommentLike ─────────────────────────────────────
        modelBuilder.Entity<CommentLike>(entity =>
        {
            entity.HasKey(cl => new { cl.UserId, cl.CommentId });

            entity.Property(cl => cl.Value)
                  .HasDefaultValue((short)1);

            entity.Property(cl => cl.CreatedAt)
                  .HasDefaultValueSql("now()");

            // FK: CommentLike.UserId → User.Id (CASCADE DELETE)
            entity.HasOne(cl => cl.User)
                  .WithMany()
                  .HasForeignKey(cl => cl.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // FK: CommentLike.CommentId → Comment.Id (CASCADE DELETE)
            entity.HasOne(cl => cl.Comment)
                  .WithMany(c => c.Likes)
                  .HasForeignKey(cl => cl.CommentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(cl => cl.CommentId);
        });

        // ── ChapterRead ─────────────────────────────────────
        modelBuilder.Entity<ChapterRead>(entity =>
        {
            entity.HasKey(cr => new { cr.UserId, cr.ChapterId });

            entity.Property(cr => cr.CreatedAt)
                  .HasDefaultValueSql("now()");

            entity.HasOne(cr => cr.User)
                  .WithMany()
                  .HasForeignKey(cr => cr.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cr => cr.Chapter)
                  .WithMany()
                  .HasForeignKey(cr => cr.ChapterId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(cr => cr.Book)
                  .WithMany()
                  .HasForeignKey(cr => cr.BookId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Быстрая выборка прочитанных глав пользователя в книге.
            entity.HasIndex(cr => new { cr.UserId, cr.BookId });
        });

        // ── Team ────────────────────────────────────────────
        modelBuilder.Entity<Team>(entity =>
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.Id)
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(t => t.Name)
                  .IsRequired()
                  .HasMaxLength(150);

            entity.Property(t => t.Slug)
                  .HasMaxLength(150);

            entity.Property(t => t.Description)
                  .HasMaxLength(2000);

            entity.Property(t => t.IsVerified).HasDefaultValue(false);

            entity.Property(t => t.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(t => t.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasIndex(t => t.Slug).IsUnique();

            // Team 1 : N Book (книга закреплена за командой; при удалении команды — TeamId = null).
            entity.HasMany(t => t.Books)
                  .WithOne(b => b.Team!)
                  .HasForeignKey(b => b.TeamId)
                  .OnDelete(DeleteBehavior.SetNull);
        });

        // ── TeamMember ──────────────────────────────────────
        modelBuilder.Entity<TeamMember>(entity =>
        {
            entity.HasKey(m => new { m.TeamId, m.UserId });

            entity.Property(m => m.Role).HasConversion<int>();
            entity.Property(m => m.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(m => m.Team)
                  .WithMany(t => t.Members)
                  .HasForeignKey(m => m.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(m => m.User)
                  .WithMany()
                  .HasForeignKey(m => m.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(m => m.UserId);
        });

        // ── TeamTitleRequest ────────────────────────────────
        modelBuilder.Entity<TeamTitleRequest>(entity =>
        {
            entity.HasKey(r => r.Id);

            entity.Property(r => r.Id)
                  .HasDefaultValueSql("gen_random_uuid()");

            entity.Property(r => r.Status).HasConversion<int>();
            entity.Property(r => r.Message).HasMaxLength(1000);
            entity.Property(r => r.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(r => r.Team)
                  .WithMany(t => t.Requests)
                  .HasForeignKey(r => r.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Book)
                  .WithMany()
                  .HasForeignKey(r => r.BookId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => new { r.BookId, r.Status });
            entity.HasIndex(r => new { r.TeamId, r.Status });
        });

        // ── TeamInvite ──────────────────────────────────────
        modelBuilder.Entity<TeamInvite>(entity =>
        {
            entity.HasKey(i => i.Id);
            entity.Property(i => i.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(i => i.Role).HasConversion<int>();
            entity.Property(i => i.Status).HasConversion<int>();
            entity.Property(i => i.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(i => i.Team)
                  .WithMany()
                  .HasForeignKey(i => i.TeamId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(i => i.User)
                  .WithMany()
                  .HasForeignKey(i => i.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(i => new { i.UserId, i.Status });
            entity.HasIndex(i => new { i.TeamId, i.Status });
        });

        // ── Notification ────────────────────────────────────
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(n => n.Id);
            entity.Property(n => n.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(n => n.Type).HasConversion<int>();
            entity.Property(n => n.Title).IsRequired().HasMaxLength(300);
            entity.Property(n => n.Message).HasMaxLength(1000);
            entity.Property(n => n.LinkUrl).HasMaxLength(500);
            entity.Property(n => n.IsRead).HasDefaultValue(false);
            entity.Property(n => n.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(n => n.User)
                  .WithMany()
                  .HasForeignKey(n => n.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Лента уведомлений пользователя, новейшие первыми (по Id UUIDv7).
            entity.HasIndex(n => new { n.UserId, n.Id });
            entity.HasIndex(n => new { n.UserId, n.IsRead });
        });

        // ── CommentReport ───────────────────────────────────
        modelBuilder.Entity<CommentReport>(entity =>
        {
            entity.HasKey(r => r.Id);
            entity.Property(r => r.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(r => r.Reason).IsRequired().HasMaxLength(500);
            entity.Property(r => r.IsResolved).HasDefaultValue(false);
            entity.Property(r => r.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(r => r.Comment)
                  .WithMany()
                  .HasForeignKey(r => r.CommentId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Reporter)
                  .WithMany()
                  .HasForeignKey(r => r.ReporterUserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => new { r.IsResolved, r.Id });
        });

        // ── ChapterVersion (ветки переводов) ────────────────
        modelBuilder.Entity<ChapterVersion>(entity =>
        {
            entity.HasKey(v => v.Id);
            entity.Property(v => v.Id).HasDefaultValueSql("gen_random_uuid()");
            entity.Property(v => v.Title).IsRequired().HasMaxLength(500);
            entity.Property(v => v.Content).IsRequired();
            entity.Property(v => v.Language).IsRequired().HasMaxLength(10).HasDefaultValue("ru");
            entity.Property(v => v.IsPublished).HasDefaultValue(true);
            entity.Property(v => v.CreatedAt).HasDefaultValueSql("now()");
            entity.Property(v => v.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(v => v.Chapter)
                  .WithMany()
                  .HasForeignKey(v => v.ChapterId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(v => v.Team)
                  .WithMany()
                  .HasForeignKey(v => v.TeamId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(v => v.CreatedBy)
                  .WithMany()
                  .HasForeignKey(v => v.CreatedByUserId)
                  .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(v => new { v.ChapterId, v.IsPublished });
        });

        // ── ChapterVersionVote ──────────────────────────────
        modelBuilder.Entity<ChapterVersionVote>(entity =>
        {
            entity.HasKey(vv => new { vv.UserId, vv.ChapterVersionId });
            entity.Property(vv => vv.Value).HasDefaultValue((short)1);
            entity.Property(vv => vv.CreatedAt).HasDefaultValueSql("now()");

            entity.HasOne(vv => vv.User)
                  .WithMany()
                  .HasForeignKey(vv => vv.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(vv => vv.ChapterVersion)
                  .WithMany(v => v.Votes)
                  .HasForeignKey(vv => vv.ChapterVersionId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(vv => vv.ChapterVersionId);
        });

        // ── UserStats (геймификация) ────────────────────
        modelBuilder.Entity<UserStats>(entity =>
        {
            entity.HasKey(s => s.UserId);
            entity.Property(s => s.Level).HasDefaultValue(1);

            entity.HasOne(s => s.User)
                  .WithOne()
                  .HasForeignKey<UserStats>(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => s.Xp);
        });

        // ── UserAchievement (геймификация) ──────────────
        modelBuilder.Entity<UserAchievement>(entity =>
        {
            entity.HasKey(a => new { a.UserId, a.Key });
            entity.Property(a => a.Key).HasMaxLength(50);

            entity.HasOne(a => a.User)
                  .WithMany()
                  .HasForeignKey(a => a.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── BookShelf (полки пользователя) ──────────────
        modelBuilder.Entity<BookShelf>(entity =>
        {
            entity.HasKey(s => new { s.UserId, s.BookId });
            entity.Property(s => s.Status).HasConversion<int>();

            entity.HasOne(s => s.User)
                  .WithMany()
                  .HasForeignKey(s => s.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(s => s.Book)
                  .WithMany()
                  .HasForeignKey(s => s.BookId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(s => new { s.UserId, s.Status });
        });

        // ── BookRating (оценки и отзывы) ────────────────
        modelBuilder.Entity<BookRating>(entity =>
        {
            entity.HasKey(r => new { r.UserId, r.BookId });
            entity.Property(r => r.Review).HasMaxLength(2000);

            entity.HasOne(r => r.User)
                  .WithMany()
                  .HasForeignKey(r => r.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(r => r.Book)
                  .WithMany(b => b.Ratings)
                  .HasForeignKey(r => r.BookId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(r => r.BookId);
        });

        // ── Follow (подписки на пользователей/команды) ──
        modelBuilder.Entity<Follow>(entity =>
        {
            entity.HasKey(f => new { f.FollowerUserId, f.TargetType, f.TargetId });
            entity.Property(f => f.TargetType).HasConversion<int>();

            entity.HasOne(f => f.Follower)
                  .WithMany()
                  .HasForeignKey(f => f.FollowerUserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Для подсчёта подписчиков цели и ленты по подпискам пользователя.
            entity.HasIndex(f => new { f.TargetType, f.TargetId });
        });

        // ── UserQuest (ежедневные квесты) ───────────────
        modelBuilder.Entity<UserQuest>(entity =>
        {
            entity.HasKey(q => new { q.UserId, q.QuestKey, q.Day });
            entity.Property(q => q.QuestKey).HasMaxLength(50);

            entity.HasOne(q => q.User)
                  .WithMany()
                  .HasForeignKey(q => q.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // ── UserNotificationPrefs (настройки уведомлений) ─
        modelBuilder.Entity<UserNotificationPrefs>(entity =>
        {
            entity.HasKey(p => p.UserId);
            entity.Property(p => p.EnableCommentReply).HasDefaultValue(true);
            entity.Property(p => p.EnableTeamInvite).HasDefaultValue(true);
            entity.Property(p => p.EnableRequestApproved).HasDefaultValue(true);
            entity.Property(p => p.EnableRequestRejected).HasDefaultValue(true);
            entity.Property(p => p.EnableNewChapter).HasDefaultValue(true);
            entity.Property(p => p.EnableMention).HasDefaultValue(true);
            entity.Property(p => p.EnableLevelUp).HasDefaultValue(true);
            entity.Property(p => p.EnableAchievement).HasDefaultValue(true);
            entity.Property(p => p.UpdatedAt).HasDefaultValueSql("now()");

            entity.HasOne(p => p.User)
                  .WithMany()
                  .HasForeignKey(p => p.UserId)
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
                if (idProp != null && idProp.Metadata.ClrType == typeof(Guid))
                {
                    var currentGuid = idProp.CurrentValue as Guid?;
                    if (!currentGuid.HasValue || currentGuid.Value == Guid.Empty)
                    {
                        idProp.CurrentValue = Guid.CreateVersion7();
                        idProp.IsTemporary = false;
                    }
                }

                var createdAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "CreatedAt");
                if (createdAtProp != null)
                {
                    var currentCreatedAt = createdAtProp.CurrentValue as DateTime?;
                    if (!currentCreatedAt.HasValue || currentCreatedAt.Value == default)
                    {
                        createdAtProp.CurrentValue = now;
                    }
                }

                var addedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "AddedAt");
                if (addedAtProp != null)
                {
                    var currentAddedAt = addedAtProp.CurrentValue as DateTime?;
                    if (!currentAddedAt.HasValue || currentAddedAt.Value == default)
                    {
                        addedAtProp.CurrentValue = now;
                    }
                }

                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null)
                {
                    var currentUpdatedAt = updatedAtProp.CurrentValue as DateTime?;
                    if (!currentUpdatedAt.HasValue || currentUpdatedAt.Value == default)
                    {
                        updatedAtProp.CurrentValue = now;
                    }
                }
            }
            else if (entry.State == EntityState.Modified)
            {
                var updatedAtProp = entry.Properties.FirstOrDefault(p => p.Metadata.Name == "UpdatedAt");
                if (updatedAtProp != null && !updatedAtProp.IsModified)
                {
                    updatedAtProp.CurrentValue = now;
                }
            }
        }
    }
}
