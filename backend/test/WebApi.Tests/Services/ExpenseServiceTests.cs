using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using WebApi.Common.Caching;
using WebApi.Common.Pagination;
using WebApi.Contracts.Expenses;
using WebApi.Domain.Entities;
using WebApi.Repositories;
using WebApi.Services;

namespace WebApi.Tests;

public sealed class ExpenseServiceTests
{
    private readonly Mock<IExpenseRepository> expenseRepository = new();
    private readonly Mock<IAuditRepository> auditRepository = new();
    private readonly Mock<ICacheService> cacheService = new();
    private readonly Mock<ILogger<ExpenseService>> logger = new();

    private ExpenseService CreateService() =>
        new(
            expenseRepository.Object,
            auditRepository.Object,
            cacheService.Object,
            logger.Object);

    #region CreateAsync
    [Fact(DisplayName = "CreateAsync should create expense, persist changes, create audit entry and invalidate expense list cache")]
    public async Task CreateAsync_ShouldCreateExpense_SaveChanges_CreateAudit_AndInvalidateListCache()
    {
        // Arrange
        var service = CreateService();

        var request = new CreateExpenseRequest
        {
            Description = "Business lunch",
            Amount = 42.50m,
            Category = "Meals",
            Date = DateTime.UtcNow
        };

        // Act
        var result = await service.CreateAsync(request, CancellationToken.None);

        // Assert
        result.Id.Should().NotBeEmpty();
        result.Description.Should().Be("Business lunch");
        result.Amount.Should().Be(42.50m);
        result.Category.Should().Be("Meals");
        result.CreatedAtUtc.Should().NotBe(default);
        result.UpdatedAtUtc.Should().NotBe(default);

        expenseRepository.Verify(
            x => x.AddAsync(
                It.Is<Expense>(e =>
                    e.Description == request.Description &&
                    e.Amount == request.Amount &&
                    e.Category == request.Category &&
                    e.CreatedAtUtc != default &&
                    e.UpdatedAtUtc != default),
                It.IsAny<CancellationToken>()),
            Times.Once);

        auditRepository.Verify(
            x => x.AddAsync(
                It.Is<AuditEntry>(a =>
                    a.Action == "Created" &&
                    a.EntityName == nameof(Expense)),
                It.IsAny<CancellationToken>()),
            Times.Once);

        expenseRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        cacheService.Verify(
            x => x.RemoveRegisteredKeysAsync(
                "expenses:list:keys",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "CreateAsync should create a 'Created' audit entry with serialized expense payload")]
    public async Task CreateAsync_ShouldCreateAuditEntry_WithCreatedActionAndSerializedChanges()
    {
        var service = CreateService();

        var request = new CreateExpenseRequest
        {
            Description = "Hotel",
            Amount = 150m,
            Category = "Accommodation",
            Date = DateTime.UtcNow
        };

        await service.CreateAsync(request, CancellationToken.None);

        auditRepository.Verify(
            x => x.AddAsync(
                It.Is<AuditEntry>(audit =>
                    audit.Action == "Created" &&
                    audit.EntityName == nameof(Expense) &&
                    audit.ChangesJson.Contains("Hotel") &&
                    audit.ChangesJson.Contains("Accommodation")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    #endregion

    #region GetByIdAsync
    [Fact(DisplayName = "GetByIdAsync should return cached expense when cache entry exists")]
    public async Task GetByIdAsync_ShouldReturnCachedExpense_WhenCacheExists()
    {
        // Arrange
        var id = Guid.NewGuid();

        var cachedExpense = new ExpenseResponse
        {
            Id = id,
            Description = "Cached expense",
            Amount = 99m,
            Category = "Software",
            Date = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        cacheService
            .Setup(x => x.GetAsync<ExpenseResponse>(
                $"expenses:{id}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedExpense);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(cachedExpense);

        expenseRepository.Verify(
            x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "GetByIdAsync should return null when expense does not exist in cache nor repository")]
    public async Task GetByIdAsync_ShouldReturnNull_WhenExpenseDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        cacheService
            .Setup(x => x.GetAsync<ExpenseResponse>(
                $"expenses:{id}",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((ExpenseResponse?)null);

        expenseRepository
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expense?)null);

        var service = CreateService();

        // Act
        var result = await service.GetByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }
    #endregion

    #region GetPagedAsync
    [Fact(DisplayName = "GetPagedAsync should return cached paged result when cache entry exists")]
    public async Task GetPagedAsync_ShouldReturnCachedResult_WhenCacheExists()
    {
        var cachedResult = new PagedResponse<ExpenseResponse>
        {
            Items = new List<ExpenseResponse>
        {
            new()
            {
                Id = Guid.NewGuid(),
                Description = "Cached expense",
                Amount = 10m,
                Category = "Meals",
                Date = DateTime.UtcNow,
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        },
            Page = 1,
            PageSize = 10,
            TotalCount = 1,
            TotalPages = 1
        };

        cacheService
            .Setup(x => x.GetAsync<PagedResponse<ExpenseResponse>>(
                It.Is<string>(key => key.Contains("expenses:list")),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedResult);

        var service = CreateService();

        var result = await service.GetPagedAsync(
            1,
            10,
            null,
            null,
            "date",
            "desc",
            CancellationToken.None);

        result.Should().BeEquivalentTo(cachedResult);

        expenseRepository.Verify(
            x => x.GetPagedAsync(
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "GetPagedAsync should query repository and cache paged result when cache is empty")]
    public async Task GetPagedAsync_ShouldQueryRepository_AndCacheResult_WhenCacheIsEmpty()
    {
        var expenses = new List<Expense>
    {
        new()
        {
            Id = Guid.NewGuid(),
            Description = "Lunch",
            Amount = 20m,
            Category = "Meals",
            Date = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        }
    };

        cacheService
            .Setup(x => x.GetAsync<PagedResponse<ExpenseResponse>>(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((PagedResponse<ExpenseResponse>?)null);

        expenseRepository
            .Setup(x => x.GetPagedAsync(
                1,
                10,
                null,
                null,
                "date",
                "desc",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((expenses, 1));

        var service = CreateService();

        var result = await service.GetPagedAsync(
            1,
            10,
            null,
            null,
            "date",
            "desc",
            CancellationToken.None);

        result.Items.Should().HaveCount(1);
        result.TotalCount.Should().Be(1);
        result.TotalPages.Should().Be(1);

        cacheService.Verify(
            x => x.SetAsync(
                It.Is<string>(key => key.Contains("expenses:list")),
                It.IsAny<PagedResponse<ExpenseResponse>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);

        cacheService.Verify(
            x => x.RegisterKeyAsync(
                "expenses:list:keys",
                It.Is<string>(key => key.Contains("expenses:list")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    #endregion

    #region UpdateAsync
    [Fact(DisplayName = "UpdateAsync should update expense, create audit entry, persist changes and invalidate related caches")]
    public async Task UpdateAsync_ShouldUpdateExpense_CreateAudit_SaveChanges_AndInvalidateCaches()
    {
        // Arrange
        var id = Guid.NewGuid();

        var existingExpense = new Expense
        {
            Id = id,
            Description = "Old description",
            Amount = 10m,
            Category = "Meals",
            Date = DateTime.UtcNow.AddDays(-1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        var request = new CreateExpenseRequest
        {
            Description = "New description",
            Amount = 25m,
            Category = "Travel",
            Date = DateTime.UtcNow
        };

        expenseRepository
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingExpense);

        var service = CreateService();

        // Act
        var result = await service.UpdateAsync(id, request, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result!.Description.Should().Be("New description");
        result.Amount.Should().Be(25m);
        result.Category.Should().Be("Travel");

        auditRepository.Verify(
            x => x.AddAsync(
                It.Is<AuditEntry>(a =>
                    a.Action == "Updated" &&
                    a.EntityId == id.ToString()),
                It.IsAny<CancellationToken>()),
            Times.Once);

        expenseRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);

        cacheService.Verify(
            x => x.RemoveAsync($"expenses:{id}", It.IsAny<CancellationToken>()),
            Times.Once);

        cacheService.Verify(
            x => x.RemoveAsync($"expenses:{id}:history", It.IsAny<CancellationToken>()),
            Times.Once);

        cacheService.Verify(
            x => x.RemoveRegisteredKeysAsync(
                "expenses:list:keys",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "UpdateAsync should return null when expense to update does not exist")]
    public async Task UpdateAsync_ShouldReturnNull_WhenExpenseDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        expenseRepository
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expense?)null);

        var service = CreateService();

        var request = new CreateExpenseRequest
        {
            Description = "New description",
            Amount = 25m,
            Category = "Travel",
            Date = DateTime.UtcNow
        };

        // Act
        var result = await service.UpdateAsync(id, request, CancellationToken.None);

        // Assert
        result.Should().BeNull();

        expenseRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "UpdateAsync should create an 'Updated' audit entry with old and new values")]
    public async Task UpdateAsync_ShouldCreateAuditEntry_WithOldAndNewValues()
    {
        var id = Guid.NewGuid();

        var existingExpense = new Expense
        {
            Id = id,
            Description = "Old expense",
            Amount = 10m,
            Category = "Meals",
            Date = DateTime.UtcNow.AddDays(-1),
            CreatedAtUtc = DateTime.UtcNow.AddDays(-2),
            UpdatedAtUtc = DateTime.UtcNow.AddDays(-2)
        };

        expenseRepository
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingExpense);

        var request = new CreateExpenseRequest
        {
            Description = "New expense",
            Amount = 20m,
            Category = "Travel",
            Date = DateTime.UtcNow
        };

        var service = CreateService();

        await service.UpdateAsync(id, request, CancellationToken.None);

        auditRepository.Verify(
            x => x.AddAsync(
                It.Is<AuditEntry>(audit =>
                    audit.Action == "Updated" &&
                    audit.EntityId == id.ToString() &&
                    audit.ChangesJson.Contains("OldValue") &&
                    audit.ChangesJson.Contains("NewValue") &&
                    audit.ChangesJson.Contains("Old expense") &&
                    audit.ChangesJson.Contains("New expense")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    #endregion

    #region DeleteByIdAsync
    [Fact(DisplayName = "DeleteByIdAsync should delete expense, create audit entry, persist changes and invalidate related caches")]
    public async Task DeleteByIdAsync_ShouldDeleteExpense_CreateAudit_SaveChanges_AndInvalidateCaches()
    {
        // Arrange
        var id = Guid.NewGuid();

        var expense = new Expense
        {
            Id = id,
            Description = "Expense to delete",
            Amount = 12m,
            Category = "Other",
            Date = DateTime.UtcNow,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        expenseRepository
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expense);

        var service = CreateService();

        // Act
        var result = await service.DeleteByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeTrue();

        expenseRepository.Verify(
            x => x.Delete(expense),
            Times.Once);

        auditRepository.Verify(
            x => x.AddAsync(
                It.Is<AuditEntry>(a =>
                    a.Action == "Deleted" &&
                    a.EntityId == id.ToString()),
                It.IsAny<CancellationToken>()),
            Times.Once);

        expenseRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact(DisplayName = "DeleteByIdAsync should return false when expense to delete does not exist")]
    public async Task DeleteByIdAsync_ShouldReturnFalse_WhenExpenseDoesNotExist()
    {
        // Arrange
        var id = Guid.NewGuid();

        expenseRepository
            .Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Expense?)null);

        var service = CreateService();

        // Act
        var result = await service.DeleteByIdAsync(id, CancellationToken.None);

        // Assert
        result.Should().BeFalse();

        expenseRepository.Verify(
            x => x.Delete(It.IsAny<Expense>()),
            Times.Never);

        expenseRepository.Verify(
            x => x.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Never);
    }
    #endregion

    #region GetHistoryAsync
    [Fact(DisplayName = "GetHistoryAsync should return cached history when cache entry exists")]
    public async Task GetHistoryAsync_ShouldReturnCachedHistory_WhenCacheExists()
    {
        var id = Guid.NewGuid();

        var cachedHistory = new List<AuditEntryResponse>
    {
        new()
        {
            Id = 1,
            EntityName = nameof(Expense),
            EntityId = id.ToString(),
            Action = "Created",
            ChangesJson = "{}",
            CreatedAtUtc = DateTime.UtcNow
        }
    };

        cacheService
            .Setup(x => x.GetAsync<IReadOnlyCollection<AuditEntryResponse>>(
                $"expenses:{id}:history",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(cachedHistory);

        var service = CreateService();

        var result = await service.GetHistoryAsync(id, CancellationToken.None);

        result.Should().BeEquivalentTo(cachedHistory);

        auditRepository.Verify(
            x => x.GetEntityHistoryAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact(DisplayName = "GetHistoryAsync should query repository and cache history when cache is empty")]
    public async Task GetHistoryAsync_ShouldQueryRepository_AndCacheHistory_WhenCacheIsEmpty()
    {
        var id = Guid.NewGuid();

        var history = new List<AuditEntry>
    {
        new()
        {
            Id = 1,
            EntityName = nameof(Expense),
            EntityId = id.ToString(),
            Action = "Created",
            ChangesJson = "{}",
            CreatedAtUtc = DateTime.UtcNow
        }
    };

        cacheService
            .Setup(x => x.GetAsync<IReadOnlyCollection<AuditEntryResponse>>(
                $"expenses:{id}:history",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((IReadOnlyCollection<AuditEntryResponse>?)null);

        auditRepository
            .Setup(x => x.GetEntityHistoryAsync(
                nameof(Expense),
                id.ToString(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var service = CreateService();

        var result = await service.GetHistoryAsync(id, CancellationToken.None);

        result.Should().HaveCount(1);
        result.First().Action.Should().Be("Created");

        cacheService.Verify(
            x => x.SetAsync(
                $"expenses:{id}:history",
                It.IsAny<IReadOnlyCollection<AuditEntryResponse>>(),
                It.IsAny<TimeSpan>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
    #endregion
}