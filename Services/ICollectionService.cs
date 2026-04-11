using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

public interface ICollectionService
{
    Task<CollectionDto> CreateCollectionAsync(Guid userId, CreateCollectionDto dto);
    Task<IEnumerable<CollectionDto>> GetUserCollectionsAsync(Guid userId);
    Task<CollectionDetailDto?> GetCollectionByIdAsync(Guid collectionId, Guid userId);
    Task<IEnumerable<Guid>> GetCollectionIdsWithBookAsync(Guid bookId, Guid userId);
    Task AddBookToCollectionAsync(Guid collectionId, Guid bookId, Guid userId);
    Task RemoveBookFromCollectionAsync(Guid collectionId, Guid bookId, Guid userId);
    Task<BookCollectionStatusDto?> GetBookCollectionStatusAsync(Guid bookId, Guid userId);
}
