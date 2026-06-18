namespace LibNode.Api.Models.Enums;

/// <summary>
/// Роль участника внутри команды переводчиков.
/// </summary>
public enum TeamRole
{
    /// <summary>Глава команды: заявки, инвайты, редактирование, полный доступ к команде.</summary>
    Head = 1,

    /// <summary>Переводчик: загрузка текста и редактирование глав.</summary>
    Translator = 2,

    /// <summary>Редактор: редактирование глав.</summary>
    Editor = 3,
}
