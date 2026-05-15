using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using WebApi.Common.Caching;
using WebApi.Common.Pagination;
using WebApi.Domain.Entities;
using WebApi.Features.Expenses.Commands.Create;
using WebApi.Features.Expenses.Commands.Delete;
using WebApi.Features.Expenses.Commands.Update;
using WebApi.Features.Expenses.DTOs;
using WebApi.Features.Expenses.Queries.Get;
using WebApi.Features.Expenses.Queries.List;
using WebApi.Infrastructure.Persistence;

namespace WebApi.Tests;

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

file static class DbFactory
{
    public static AppDbContext Create() =>
        new(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
}

// ---------------------------------------------------------------------------
// CreateExpenseCommandHandler
// ---------------------------------------------------------------------------

public sealed class CreateExpenseCommandHandlerTests
{
    private readonly Mock<ICacheService> _cache = new();

    private CreateExpenseCommandHandler Handler(AppDbContext ctx) =>
        new(ctx, _cache.Object, NullLogger<CreateExpenseCommandHandler>.Instance);

    [Fact(DisplayName = "Handle should persist the expense and return a non-empty Guid")]
    public async Task Handle_ShouldPersistExpense_AndReturnNewId()
    {
        using var ctx = DbFactory.Create();

        var id = await Handler(ctx).Handle(
            new CreateExpenseCommand("Business lunch", 42.5m, "Meals", DateTime.UtcNow),
            CancellationToken.None);

        id.Should().NotBeEmpty();
        var saved = await ctx.Expenses.FindAsync(id);
        saved.Should().NotBeNull();
        saved!.Description.Should().Be("Business lunch");
        saved.Amount.Should().Be(42.5m);
        saved.Category.Should().Be("Meals");
        saved.CreatedAtUtc.Should().NotBe(default);
    }

    [Fact(DisplayName = "Handle should trim whitespace from Description and Category")]
    public async Task Handle_ShouldTrimWhitespace()
    {
        using var ctx = DbFactory.Create();

        var id = await Handler(ctx).Handle(
            new CreateExpenseCommand("  Hotel stay  ", 100.0m, "  Travel  ", DateTime.UtcNow),
            CancellationToken.None);

        var saved = await ctx.Expenses.FindAsync(id);
        saved!.Description.Should().Be("Hotel stay");
        saved.Category.Should().Be("Travel");
    }

    [Fact(DisplayName = "Handle should invalidate the expense list cache")]
    public async Task Handle_ShouldInvalidateListCache()
    {
        using var ctx = DbFactory.Create();

        await Handler(ctx).Handle(
            new CreateExpenseCommand("Lunch", 15.0m, "Meals", DateTime.UtcNow),
            CancellationToken.None);

        _cache.Verify(
            x => x.RemoveRegisteredKeysAsync("expenses:list:keys", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    // BUG: UpdatedAtUtc is never set on creation — it stays at default(DateTime).
    [Fact(DisplayName = "Handle should set CreatedAtUtc — UpdatedAtUtc is left at default (known bug)")]
    public async Task Handle_UpdatedAtUtcIsNotSet_KnownBug()
    {
        using var ctx = DbFactory.Create();

        var id = await Handler(ctx).Handle(
            new CreateExpenseCommand("Test", 10.0m, "Other", DateTime.UtcNow),
            CancellationToken.None);

        var saved = await ctx.Expenses.FindAsync(id);
        saved!.CreatedAtUtc.Should().NotBe(default);
        saved.UpdatedAtUtc.Should().Be(default); // should equal CreatedAtUtc once fixed
    }
}

// ---------------------------------------------------------------------------
// DeleteExpenseCommandHandler
// ---------------------------------------------------------------------------

public sealed class DeleteExpenseCommandHandlerTests
{
    [Fact(DisplayName = "Handle should remove the expense from the database when it exists")]
    public async Task Handle_ShouldDeleteExpense_WhenFound()
    {
        using var ctx = DbFactory.Create();
        var expense = Seed(ctx);

        await new DeleteExpenseCommandHandler(ctx)
            .Handle(new DeleteExpenseCommand(expense.Id), CancellationToken.None);

        (await ctx.Expenses.FindAsync(expense.Id)).Should().BeNull();
    }

    // BUG: the handler silently swallows a missing expense; the controller always
    // returns 204, so DELETE on a non-existent ID never returns 404.
    [Fact(DisplayName = "Handle should complete without throwing when expense not found (known silent-404 bug)")]
    public async Task Handle_ShouldNotThrow_WhenExpenseNotFound()
    {
        using var ctx = DbFactory.Create();

        var act = async () =>
            await new DeleteExpenseCommandHandler(ctx)
                .Handle(new DeleteExpenseCommand(Guid.NewGuid()), CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    private static Expense Seed(AppDbContext ctx)
    {
        var e = new Expense
        {
            Id = Guid.NewGuid(),
            Description = "To delete",
            Amount = 10m,
            Category = "Other",
            Date = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        ctx.Expenses.Add(e);
        ctx.SaveChanges();
        return e;
    }
}

// ---------------------------------------------------------------------------
// UpdateExpenseCommandHandler
// ---------------------------------------------------------------------------

public sealed class UpdateExpenseCommandHandlerTests
{
    [Fact(DisplayName = "Handle should update the expense and return true when it exists")]
    public async Task Handle_ShouldUpdateExpense_AndReturnTrue_WhenFound()
    {
        using var ctx = DbFactory.Create();
        var expense = Seed(ctx);

        var result = await new UpdateExpenseCommandHandler(ctx).Handle(
            new UpdateExpenseCommand(expense.Id, "New description", 99.9m, "Travel", DateTime.UtcNow),
            CancellationToken.None);

        result.Should().BeTrue();
        var updated = await ctx.Expenses.FindAsync(expense.Id);
        updated!.Description.Should().Be("New description");
        updated.Amount.Should().Be(99.9m);
        updated.Category.Should().Be("Travel");
    }

    [Fact(DisplayName = "Handle should return false when the expense does not exist")]
    public async Task Handle_ShouldReturnFalse_WhenExpenseNotFound()
    {
        using var ctx = DbFactory.Create();

        var result = await new UpdateExpenseCommandHandler(ctx).Handle(
            new UpdateExpenseCommand(Guid.NewGuid(), "X", 1.0m, "Other", DateTime.UtcNow),
            CancellationToken.None);

        result.Should().BeFalse();
    }

    private static Expense Seed(AppDbContext ctx)
    {
        var e = new Expense
        {
            Id = Guid.NewGuid(),
            Description = "Old description",
            Amount = 10m,
            Category = "Meals",
            Date = DateTime.UtcNow.AddDays(-1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-1)
        };
        ctx.Expenses.Add(e);
        ctx.SaveChanges();
        return e;
    }
}

// ---------------------------------------------------------------------------
// GetExpenseQueryHandler
// ---------------------------------------------------------------------------

public sealed class GetExpenseQueryHandlerTests
{
    [Fact(DisplayName = "Handle should return an ExpenseDto when the expense exists")]
    public async Task Handle_ShouldReturnExpenseDto_WhenFound()
    {
        using var ctx = DbFactory.Create();
        var expense = Seed(ctx);

        var result = await new GetExpenseQueryHandler(ctx)
            .Handle(new GetExpenseQuery(expense.Id), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be(expense.Id);
        result.Description.Should().Be("Conference ticket");
        result.Amount.Should().Be(299m);
        result.Category.Should().Be("Software");
    }

    [Fact(DisplayName = "Handle should return null when the expense does not exist")]
    public async Task Handle_ShouldReturnNull_WhenExpenseNotFound()
    {
        using var ctx = DbFactory.Create();

        var result = await new GetExpenseQueryHandler(ctx)
            .Handle(new GetExpenseQuery(Guid.NewGuid()), CancellationToken.None);

        result.Should().BeNull();
    }

    private static Expense Seed(AppDbContext ctx)
    {
        var e = new Expense
        {
            Id = Guid.NewGuid(),
            Description = "Conference ticket",
            Amount = 299m,
            Category = "Software",
            Date = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };
        ctx.Expenses.Add(e);
        ctx.SaveChanges();
        return e;
    }
}

// ---------------------------------------------------------------------------
// ListExpenseQueryHandler
// ---------------------------------------------------------------------------

public sealed class ListExpenseQueryHandlerTests
{
    private readonly Mock<ICacheService> _cache = new();

    private ListExpenseQueryHandler Handler(AppDbContext ctx) =>
        new(ctx, _cache.Object, NullLogger<ListExpenseQueryHandler>.Instance);

    [Fact(DisplayName = "Handle should return the cached result without hitting the database on cache hit")]
    public async Task Handle_ShouldReturnCachedResult_OnCacheHit()
    {
        var cached = new PagedResponse<ExpenseDto>
        {
            Items = [new ExpenseDto(Guid.NewGuid(), "Cached", 10m, "Meals",
                DateTime.UtcNow, DateTime.UtcNow, DateTime.UtcNow)],
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            TotalPages = 1
        };

        _cache
            .Setup(x => x.GetAsync<PagedResponse<ExpenseDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(cached);

        using var ctx = DbFactory.Create();

        var result = await Handler(ctx).Handle(new ListExpenseQuery(), CancellationToken.None);

        result.Should().BeEquivalentTo(cached);
    }

    [Fact(DisplayName = "Handle should query the database and return a paged result on cache miss")]
    public async Task Handle_ShouldQueryDatabase_AndReturnPagedResult_OnCacheMiss()
    {
        _cache
            .Setup(x => x.GetAsync<PagedResponse<ExpenseDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResponse<ExpenseDto>?)null);

        using var ctx = DbFactory.Create();
        ctx.Expenses.AddRange(
            new Expense { Id = Guid.NewGuid(), Description = "Lunch", Amount = 20m, Category = "Meals", Date = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
            new Expense { Id = Guid.NewGuid(), Description = "Train", Amount = 80m, Category = "Travel", Date = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new ListExpenseQuery(Page: 1, PageSize: 10), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
        result.Page.Should().Be(1);
    }

    [Fact(DisplayName = "Handle should filter results by category when Category is specified")]
    public async Task Handle_ShouldFilterByCategory()
    {
        _cache
            .Setup(x => x.GetAsync<PagedResponse<ExpenseDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResponse<ExpenseDto>?)null);

        using var ctx = DbFactory.Create();
        ctx.Expenses.AddRange(
            new Expense { Id = Guid.NewGuid(), Description = "Lunch", Amount = 20m, Category = "Meals", Date = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow },
            new Expense { Id = Guid.NewGuid(), Description = "Train", Amount = 80m, Category = "Travel", Date = DateTime.UtcNow, CreatedAtUtc = DateTime.UtcNow, UpdatedAtUtc = DateTime.UtcNow });
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new ListExpenseQuery(Category: "Meals"), CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.Items.ElementAt(0).Category.Should().Be("Meals");
    }

    [Fact(DisplayName = "Handle should apply pagination correctly")]
    public async Task Handle_ShouldPaginate_Correctly()
    {
        _cache
            .Setup(x => x.GetAsync<PagedResponse<ExpenseDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResponse<ExpenseDto>?)null);

        using var ctx = DbFactory.Create();
        for (var i = 1; i <= 5; i++)
            ctx.Expenses.Add(new Expense
            {
                Id = Guid.NewGuid(),
                Description = $"Expense {i}",
                Amount = i * 10m,
                Category = "Other",
                Date = DateTime.UtcNow.AddDays(-i),
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            });
        await ctx.SaveChangesAsync();

        var result = await Handler(ctx).Handle(new ListExpenseQuery(Page: 2, PageSize: 2), CancellationToken.None);

        result.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(5);
        result.TotalPages.Should().Be(3);
        result.Page.Should().Be(2);
    }

    // BUG: after a cache miss the handler never calls SetAsync, so the result is
    // never cached and every subsequent identical request hits the database.
    [Fact(DisplayName = "Handle should NOT write to cache after a DB query — known missing-cache-write bug")]
    public async Task Handle_ShouldNotWriteToCache_AfterCacheMiss_KnownBug()
    {
        _cache
            .Setup(x => x.GetAsync<PagedResponse<ExpenseDto>>(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResponse<ExpenseDto>?)null);

        using var ctx = DbFactory.Create();

        await Handler(ctx).Handle(new ListExpenseQuery(), CancellationToken.None);

        _cache.Verify(
            x => x.SetAsync(
                It.IsAny<string>(),
                It.IsAny<PagedResponse<ExpenseDto>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Never); // passes today due to the bug — flip to Times.Once once fixed
    }
}