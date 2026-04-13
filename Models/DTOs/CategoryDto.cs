namespace LibNode.Api.Models.DTOs;

public record CategoryDto(
    Guid Id,
    string Name,
    string Slug
);
