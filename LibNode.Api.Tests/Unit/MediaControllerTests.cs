using System.Security.Claims;
using LibNode.Api.Controllers;
using LibNode.Api.Models.Common;
using LibNode.Api.Models.DTOs;
using LibNode.Api.Models.Enums;
using LibNode.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace LibNode.Api.Tests.Unit;

public class MediaControllerTests
{
    private static FormFile CreateImageFile()
    {
        var stream = new MemoryStream([1, 2, 3, 4]);
        return new FormFile(stream, 0, stream.Length, "file", "image.png")
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/png",
        };
    }

    private static MediaController CreateController(
        RecordingStorageService storage,
        IAuthService? auth = null,
        IBookService? books = null,
        ITeamService? teams = null,
        Guid? userId = null)
    {
        var controller = new MediaController(
            storage,
            new StubImageService(),
            auth ?? new StubAuthService(),
            books ?? new StubBookService(),
            teams ?? new StubTeamService(),
            NullLogger<MediaController>.Instance);

        var principal = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim(ClaimTypes.NameIdentifier, (userId ?? Guid.NewGuid()).ToString())],
            "test"));
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };

        return controller;
    }

    [Fact]
    public async Task UploadAvatar_WhenUserMissing_DeletesNewUpload()
    {
        var storage = new RecordingStorageService();
        var controller = CreateController(storage, auth: new MissingUserAuthService());

        var result = await controller.UploadAvatar(CreateImageFile(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(storage.Uploaded, storage.Deleted);
    }

    [Fact]
    public async Task UploadCover_WhenBookMissing_DeletesNewUpload()
    {
        var storage = new RecordingStorageService();
        var controller = CreateController(storage, books: new MissingBookService(), teams: new AllowTeamService());

        var result = await controller.UploadCover(Guid.NewGuid(), CreateImageFile(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Equal(storage.Uploaded, storage.Deleted);
    }

    private sealed class RecordingStorageService : IStorageService
    {
        public bool IsConfigured => true;
        public string PublicBase => "https://storage.test/";
        public List<string> Uploaded { get; } = [];
        public List<string> Deleted { get; } = [];

        public Task<string> UploadAsync(Stream content, string contentType, string keyPrefix, string fileExtension, CancellationToken ct = default)
        {
            var key = $"{keyPrefix.Trim('/')}/{Uploaded.Count + 1}.{fileExtension.TrimStart('.')}";
            Uploaded.Add(key);
            return Task.FromResult(key);
        }

        public string ResolveUrl(string? key) => key is null ? string.Empty : $"https://storage.test/{key}";

        public Task DeleteAsync(string? key, CancellationToken ct = default)
        {
            if (!string.IsNullOrWhiteSpace(key)) Deleted.Add(key);
            return Task.CompletedTask;
        }

        public Task<(long ObjectCount, long TotalBytes)> GetStatsAsync(CancellationToken ct = default)
            => Task.FromResult((0L, 0L));
    }

    private sealed class StubImageService : IImageService
    {
        public Task<MemoryStream> MakeThumbnailWebpAsync(Stream source, int maxSize, CancellationToken ct = default)
            => Task.FromResult(new MemoryStream([5, 6, 7, 8]));

        public Task<MemoryStream> ReencodeWebpAsync(Stream source, int maxDimension, CancellationToken ct = default)
            => Task.FromResult(new MemoryStream([1, 2, 3, 4]));
    }

    private class StubAuthService : IAuthService
    {
        public Task<AuthResponseDto> RegisterAsync(CreateUserDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuthResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<UserProfileDto?> GetProfileAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<AuthResponseDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task ChangePasswordAsync(Guid userId, ChangePasswordDto dto, CancellationToken ct = default) => throw new NotImplementedException();

        public virtual Task<(string? AvatarKey, string? AvatarThumbKey)?> UpdateAvatarKeysAsync(
            Guid userId,
            string avatarKey,
            string? avatarThumbKey,
            CancellationToken ct = default)
            => Task.FromResult<(string?, string?)?>(("avatars/old.webp", "avatars/thumbs/old.webp"));
    }

    private sealed class MissingUserAuthService : StubAuthService
    {
        public override Task<(string? AvatarKey, string? AvatarThumbKey)?> UpdateAvatarKeysAsync(
            Guid userId,
            string avatarKey,
            string? avatarThumbKey,
            CancellationToken ct = default)
            => Task.FromResult<(string?, string?)?>(null);
    }

    private class StubBookService : IBookService
    {
        public Task<CursorStringPagedResult<BookDto>> GetAllAsync(GetBooksQueryDto query, Guid? userId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<PagedResult<BookDto>> GetAllWithOffsetAsync(GetBooksQueryDto query, Guid? userId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<BookDetailDto?> GetByIdAsync(Guid id, Guid? userId = null, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<BookDto> CreateAsync(CreateBookDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<BookDto?> UpdateAsync(Guid id, UpdateBookDto dto, CancellationToken ct = default) => throw new NotImplementedException();

        public virtual Task<(string? CoverKey, string? CoverThumbKey)?> UpdateCoverKeysAsync(
            Guid id,
            string coverKey,
            string? coverThumbKey,
            CancellationToken ct = default)
            => Task.FromResult<(string?, string?)?>(("covers/old.webp", "covers/thumbs/old.webp"));
    }

    private sealed class MissingBookService : StubBookService
    {
        public override Task<(string? CoverKey, string? CoverThumbKey)?> UpdateCoverKeysAsync(
            Guid id,
            string coverKey,
            string? coverThumbKey,
            CancellationToken ct = default)
            => Task.FromResult<(string?, string?)?>(null);
    }

    private class StubTeamService : ITeamService
    {
        public Task<IReadOnlyList<TeamDto>> ListTeamsAsync(CancellationToken ct = default) => throw new NotImplementedException();
        public Task<TeamDto> CreateTeamAsync(CreateTeamDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<TeamDto?> UpdateTeamAsync(Guid teamId, UpdateTeamDto dto, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<bool> DeleteTeamAsync(Guid teamId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<TeamDto?> ToggleVerifyAsync(Guid teamId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<TeamTitleRequestDto>> ListAllRequestsAsync(RequestStatus? status, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<TeamTitleRequestDto?> DecideRequestAsync(Guid requestId, bool approve, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<TeamDetailDto?> GetTeamAsync(Guid teamId, Guid? currentUserId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<TeamDto>> GetMyTeamsAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task UpdateMemberRoleAsync(Guid teamId, Guid actingUserId, bool isAdmin, Guid targetUserId, TeamRole role, CancellationToken ct = default) => throw new NotImplementedException();
        public Task RemoveMemberAsync(Guid teamId, Guid actingUserId, bool isAdmin, Guid targetUserId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task CreateInviteAsync(Guid teamId, Guid actingUserId, bool isAdmin, string username, TeamRole role, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<IReadOnlyList<TeamInviteDto>> GetMyInvitesAsync(Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task AcceptInviteAsync(Guid inviteId, Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task DeclineInviteAsync(Guid inviteId, Guid userId, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<TeamTitleRequestDto> RequestTitleAsync(Guid teamId, Guid actingUserId, bool isAdmin, Guid bookId, string? message, CancellationToken ct = default) => throw new NotImplementedException();
        public Task<TeamRole?> GetUserRoleInBookTeamAsync(Guid userId, Guid bookId, CancellationToken ct = default) => throw new NotImplementedException();

        public virtual Task<bool> CanEditBookChaptersAsync(Guid userId, Guid bookId, bool isAdmin, CancellationToken ct = default) => throw new NotImplementedException();
        public virtual Task<bool> CanManageBookTitleAsync(Guid userId, Guid bookId, bool isAdmin, CancellationToken ct = default) => Task.FromResult(false);
        public Task<BookTeamDto?> GetBookTeamAsync(Guid bookId, CancellationToken ct = default) => throw new NotImplementedException();
    }

    private sealed class AllowTeamService : StubTeamService
    {
        public override Task<bool> CanManageBookTitleAsync(Guid userId, Guid bookId, bool isAdmin, CancellationToken ct = default)
            => Task.FromResult(true);
    }
}
