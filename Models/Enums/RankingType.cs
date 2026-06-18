namespace LibNode.Api.Models.Enums;

/// <summary>Тип рейтинга книг для страницы /rankings.</summary>
public enum RankingType
{
    Popular = 1,      // по просмотрам
    TopRated = 2,     // по средней оценке
    MostChapters = 3, // по числу опубликованных глав
    Newest = 4,       // по дате создания
}
