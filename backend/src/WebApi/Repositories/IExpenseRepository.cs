using WebApi.Domain.Entities;

namespace WebApi.Repositories;

/// <summary>
/// Provides data access operations for expenses.
/// The repository abstracts Entity Framework Core access from the service layer.
/// </summary>
public interface IExpenseRepository
{
    /// <summary>
    /// Adds a new expense to the current persistence context.
    /// Changes are not committed until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="expense">The expense entity to add.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    Task AddAsync(
        Expense expense,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves an expense by its unique identifier.
    /// </summary>
    /// <param name="id">The expense identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// The matching expense entity, or <c>null</c> when no expense exists for the provided identifier.
    /// </returns>
    Task<Expense?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a filtered, sorted and paginated list of expenses.
    /// Filtering, sorting and pagination should be applied at database query level
    /// to avoid loading unnecessary records into memory.
    /// </summary>
    /// <param name="page">The page number to retrieve.</param>
    /// <param name="pageSize">The number of items per page.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="search">Optional search term.</param>
    /// <param name="sortBy">Optional field used for sorting.</param>
    /// <param name="sortDirection">Optional sort direction, usually "asc" or "desc".</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A tuple containing the current page items and the total number of matching records before pagination.
    /// </returns>
    Task<(IReadOnlyCollection<Expense> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? category,
        string? search,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken);

    /// <summary>
    /// Marks an expense for deletion in the current persistence context.
    /// Changes are not committed until <see cref="SaveChangesAsync"/> is called.
    /// </summary>
    /// <param name="entity">The expense entity to delete.</param>
    void Delete(Expense entity);

    /// <summary>
    /// Persists all pending changes in the current persistence context.
    /// </summary>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>The number of state entries written to the database.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}