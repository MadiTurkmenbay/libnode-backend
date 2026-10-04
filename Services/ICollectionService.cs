using LibNode.Api.Models.DTOs;

namespace LibNode.Api.Services;

public interface ICollectionService
{
    Task<CollectionDto> CreateCollectionAsync(Guid userId, CreateCollectionDto dto);
    Task<CollectionDto?> RenameCollectionAsync(Guid collectionId, Guid userId, CreateCollectionDto dto, CancellationToken ct = default);
    Task<bool> DeleteCollectionAsync(Guid collectionId, Guid userId, CancellationToken ct = default);
    Task<IEnumerable<CollectionDto>> GetUserCollectionsAsync(Guid userId);
    Task<CollectionDetailDto?> GetCollectionByIdAsync(Guid collectionId, Guid userId);
    Task<IEnumerable<Guid>> GetCollectionIdsWithBookAsync(Guid bookId, Guid userId);
    Task AddBookToCollectionAsync(Guid collectionId, Guid bookId, Guid userId, CancellationToken ct = default);
    Task RemoveBookFromCollectionAsync(Guid collectionId, Guid bookId, Guid userId);
    Task<BookCollectionStatusDto?> GetBookCollectionStatusAsync(Guid bookId, Guid userId);
}
