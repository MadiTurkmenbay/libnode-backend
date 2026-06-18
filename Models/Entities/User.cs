namespace LibNode.Api.Models.Entities;

/// <summary>
/// Сущность пользователя. Поддерживает роли: Admin, User, Translator.
/// </summary>
public class User
{
    public Guid Id { get; set; }

    /// <summary>Уникальное имя пользователя.</summary>
    public required string Username { get; set; }

    /// <summary>Email (используется для входа).</summary>
    public required string Email { get; set; }

    /// <summary>BCrypt-хэш пароля.</summary>
    public required string PasswordHash { get; set; }

    /// <summary>
    /// Метка безопасности (GUID). Включается в JWT и сверяется при валидации токена.
    /// Пересоздаётся при регистрации, смене пароля, бане и смене роли, что инвалидирует
    /// все ранее выданные токены пользователя.
    /// </summary>
    public string SecurityStamp { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>Роль пользователя: "Admin", "User", "Translator".</summary>
    public string Role { get; set; } = "User";

    /// <summary>URL аватара (необязательно).</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>URL уменьшенной копии аватара (превью).</summary>
    public string? AvatarThumbUrl { get; set; }

    /// <summary>Краткое описание «о себе» (необязательно).</summary>
    public string? Bio { get; set; }

    public bool IsBanned { get; set; } = false;
    public bool IsMuted { get; set; } = false;

    public DateTime CreatedAt { get; set; }

    /// <summary>Пользовательские коллекции книг.</summary>
    public ICollection<UserCollection> Collections { get; set; } = new List<UserCollection>();

    /// <summary>Лайки глав.</summary>
    public ICollection<ChapterLike> ChapterLikes { get; set; } = new List<ChapterLike>();
    public ICollection<ReadingProgress> ReadingProgresses { get; set; } = new List<ReadingProgress>();
}
