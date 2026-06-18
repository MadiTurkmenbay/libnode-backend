using LibNode.Api.Data;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Entities;
using LibNode.Api.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace LibNode.Api.Services;

public class QuestService : IQuestService
{
    private enum QuestAction { Read, Comment }

    private sealed record Def(string Key, string Title, string Icon, QuestAction Action, int Target, int Reward);

    private static readonly Def[] Catalog =
    [
        new("read-1", "Прочитать 1 главу", "BookOpen", QuestAction.Read, 1, 15),
        new("read-3", "Прочитать 3 главы", "BookMarked", QuestAction.Read, 3, 30),
        new("comment-1", "Оставить комментарий", "MessageSquare", QuestAction.Comment, 1, 10),
    ];

    private readonly AppDbContext _db;
    private readonly INotificationService _notifications;

    public QuestService(AppDbContext db, INotificationService notifications)
    {
        _db = db;
        _notifications = notifications;
    }

    public Task<int> RecordChapterReadAsync(Guid userId, CancellationToken ct = default)
        => RecordAsync(userId, QuestAction.Read, ct);

    public Task<int> RecordCommentAsync(Guid userId, CancellationToken ct = default)
        => RecordAsync(userId, QuestAction.Comment, ct);

    private async Task<int> RecordAsync(Guid userId, QuestAction action, CancellationToken ct)
    {
        var defs = Catalog.Where(d => d.Action == action).ToArray();
        if (defs.Length == 0) return 0;

        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var keys = defs.Select(d => d.Key).ToList();

        var bonus = 0;
        var completed = new List<Def>();

        // Read-then-insert гонка по составному PK (UserId, QuestKey, Day):
        // два конкурентных первых вызова оба вставляют недостающую строку → 23505.
        // Идемпотентность как в ReadingProgress/ReaderIngest: ловим UniqueViolation,
        // откатываем трекинг и перечитываем актуальные строки, затем повторяем.
        while (true)
        {
            bonus = 0;
            completed.Clear();

            var rows = await _db.UserQuests
                .Where(q => q.UserId == userId && q.Day == day && keys.Contains(q.QuestKey))
                .ToListAsync(ct);

            foreach (var def in defs)
            {
                var row = rows.FirstOrDefault(r => r.QuestKey == def.Key);
                if (row == null)
                {
                    row = new UserQuest { UserId = userId, QuestKey = def.Key, Day = day, Progress = 0, Rewarded = false };
                    _db.UserQuests.Add(row);
                }
                if (row.Rewarded) continue;

                row.Progress++;
                if (row.Progress >= def.Target)
                {
                    row.Rewarded = true;
                    bonus += def.Reward;
                    completed.Add(def);
                }
            }

            try
            {
                await _db.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException ex) when (IsUniqueViolation(ex))
            {
                // Конкурент успел вставить строку(и) первым. Сбрасываем трекинг текущей
                // попытки (наши Add/Modify) и перечитываем актуальные строки на следующей
                // итерации — уже учтя зафиксированный конкурентом прогресс.
                foreach (var entry in _db.ChangeTracker.Entries<UserQuest>().ToList())
                {
                    entry.State = EntityState.Detached;
                }
            }
        }

        foreach (var def in completed)
        {
            await _notifications.CreateAsync(
                userId,
                NotificationType.Achievement,
                $"Квест выполнен: {def.Title}",
                $"+{def.Reward} XP",
                "/profile",
                ct);
        }

        return bonus;
    }

    public async Task<IReadOnlyList<QuestDto>> GetForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var day = DateOnly.FromDateTime(DateTime.UtcNow);
        var rows = await _db.UserQuests.AsNoTracking()
            .Where(q => q.UserId == userId && q.Day == day)
            .ToDictionaryAsync(q => q.QuestKey, ct);

        return Catalog.Select(d =>
        {
            rows.TryGetValue(d.Key, out var row);
            var progress = Math.Min(row?.Progress ?? 0, d.Target);
            return new QuestDto(d.Key, d.Title, d.Icon, d.Target, progress, d.Reward, row?.Rewarded ?? false);
        }).ToList();
    }

    private static bool IsUniqueViolation(DbUpdateException exception) =>
        exception.InnerException is PostgresException postgresException
        && postgresException.SqlState == PostgresErrorCodes.UniqueViolation;
}
