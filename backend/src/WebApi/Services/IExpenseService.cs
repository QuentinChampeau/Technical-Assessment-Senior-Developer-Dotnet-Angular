using WebApi.Common.Pagination;
using WebApi.Contracts.Expenses;

namespace WebApi.Services;

/// <summary>
/// Defines business operations for managing expenses.
/// The service layer coordinates validation, persistence, caching, audit logging,
/// and mapping between domain entities and API contracts.
/// </summary>
public interface IExpenseService
{
    /// <summary>
    /// Creates a new expense and records the corresponding audit entry.
    /// </summary>
    /// <param name="request">The expense creation request.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>The created expense response.</returns>
    /// <exception cref="ArgumentException">
    /// Can be thrown if the request contains invalid business data before persistence.
    /// </exception>
    Task<ExpenseResponse> CreateAsync(
        CreateExpenseRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves expenses using pagination, optional filtering, search and sorting.
    /// </summary>
    /// <param name="page">The requested page number. Values lower than 1 are normalized by the implementation.</param>
    /// <param name="pageSize">The number of items per page. The implementation may enforce a maximum value.</param>
    /// <param name="category">Optional category filter.</param>
    /// <param name="search">Optional search term applied to expense text fields.</param>
    /// <param name="sortBy">Optional field used for sorting.</param>
    /// <param name="sortDirection">Optional sort direction, usually "asc" or "desc".</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>A paginated response containing expense results and pagination metadata.</returns>
    Task<PagedResponse<ExpenseResponse>> GetPagedAsync(
        int page,
        int pageSize,
        string? category,
        string? search,
        string? sortBy,
        string? sortDirection,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves a single expense by its unique identifier.
    /// </summary>
    /// <param name="id">The expense identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// The matching expense response, or <c>null</c> when no expense exists for the provided identifier.
    /// </returns>
    Task<ExpenseResponse?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates an existing expense and records the corresponding audit entry.
    /// </summary>
    /// <param name="id">The identifier of the expense to update.</param>
    /// <param name="request">The updated expense data.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// The updated expense response, or <c>null</c> when no expense exists for the provided identifier.
    /// </returns>
    Task<ExpenseResponse?> UpdateAsync(
        Guid id,
        CreateExpenseRequest request,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an existing expense and records the corresponding audit entry.
    /// </summary>
    /// <param name="id">The identifier of the expense to delete.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// <c>true</c> when the expense was deleted; otherwise <c>false</c> when no matching expense exists.
    /// </returns>
    Task<bool> DeleteByIdAsync(
        Guid id,
        CancellationToken cancellationToken);

    /// <summary>
    /// Retrieves the audit history for a specific expense.
    /// </summary>
    /// <param name="id">The expense identifier.</param>
    /// <param name="cancellationToken">Token used to cancel the asynchronous operation.</param>
    /// <returns>
    /// A collection of audit entries associated with the expense. Returns an empty collection when no history exists.
    /// </returns>
    Task<IReadOnlyCollection<AuditEntryResponse>> GetHistoryAsync(
        Guid id,
        CancellationToken cancellationToken);
}