using LibNode.Api.Data;
using LibNode.Api.Models.Entities;
using LibNode.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibNode.Api.Tests.Unit;

public class ChapterServiceTests
{
    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Book CreateBook(string title)
    {
        return new Book { Id = Guid.NewGuid(), Title = title };
    }

    private static Chapter CreateChapter(Guid bookId, int number, string title)
    {
        return new Chapter
        {
            Id = Guid.NewGuid(),
            BookId = bookId,
            ChapterNumber = number,
            Title = title,
            Content = "content"
        };
    }

    [Fact]
    public async Task GetByIdAsync_FirstChapter_HasNullPreviousAndNextId()
    {
        await using var context = CreateInMemoryContext();
        var book = CreateBook("Single Chapter Book");
        var chapter = CreateChapter(book.Id, 1, "Chapter 1");
        context.Books.Add(book);
        context.Chapters.Add(chapter);
        await context.SaveChangesAsync();

        var service = new ChapterService(context);
        var result = await service.GetByIdAsync(chapter.Id);

        Assert.NotNull(result);
        Assert.Null(result.PreviousChapterId);
        Assert.Null(result.NextChapterId);
    }

    [Fact]
    public async Task GetByIdAsync_NonContiguousNumbers_FindsNeighborsByRank()
    {
        await using var context = CreateInMemoryContext();
        var book = CreateBook("Non-contiguous Book");
        var chapters = new[]
        {
            CreateChapter(book.Id, 1, "Chapter 1"),
            CreateChapter(book.Id, 2, "Chapter 2"),
            CreateChapter(book.Id, 5, "Chapter 5"),
            CreateChapter(book.Id, 10, "Chapter 10"),
        };
        context.Books.Add(book);
        context.Chapters.AddRange(chapters);
        await context.SaveChangesAsync();

        var service = new ChapterService(context);
        var result = await service.GetByIdAsync(chapters[2].Id);

        Assert.NotNull(result);
        Assert.Equal(chapters[1].Id, result.PreviousChapterId);
        Assert.Equal(chapters[3].Id, result.NextChapterId);
    }

    [Fact]
    public async Task GetByIdAsync_LastChapter_HasNullNextId()
    {
        await using var context = CreateInMemoryContext();
        var book = CreateBook("Last Chapter Book");
        var chapters = new[]
        {
            CreateChapter(book.Id, 1, "Chapter 1"),
            CreateChapter(book.Id, 2, "Chapter 2"),
        };
        context.Books.Add(book);
        context.Chapters.AddRange(chapters);
        await context.SaveChangesAsync();

        var service = new ChapterService(context);
        var result = await service.GetByIdAsync(chapters[1].Id);

        Assert.NotNull(result);
        Assert.Equal(chapters[0].Id, result.PreviousChapterId);
        Assert.Null(result.NextChapterId);
    }

    [Fact]
    public async Task GetByIdAsync_ChapterFromAnotherBook_DoesNotAffectNeighbors()
    {
        await using var context = CreateInMemoryContext();
        var bookA = CreateBook("Book A");
        var bookB = CreateBook("Book B");
        var chapterA1 = CreateChapter(bookA.Id, 1, "A Chapter 1");
        var chapterA2 = CreateChapter(bookA.Id, 2, "A Chapter 2");
        var chapterB1 = CreateChapter(bookB.Id, 1, "B Chapter 1");
        context.Books.AddRange(bookA, bookB);
        context.Chapters.AddRange(chapterA1, chapterA2, chapterB1);
        await context.SaveChangesAsync();

        var service = new ChapterService(context);
        var result = await service.GetByIdAsync(chapterA2.Id);

        Assert.NotNull(result);
        Assert.Equal(chapterA1.Id, result.PreviousChapterId);
        Assert.Null(result.NextChapterId);
    }
}
