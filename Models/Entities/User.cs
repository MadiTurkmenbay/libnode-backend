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

    /// <summary>Роль пользователя: "Admin", "User", "Translator".</summary>
    public string Role { get; set; } = "User";

    public DateTime CreatedAt { get; set; }

    /// <summary>Пользовательские коллекции книг.</summary>
    public ICollection<UserCollection> Collections { get; set; } = new List<UserCollection>();
}
