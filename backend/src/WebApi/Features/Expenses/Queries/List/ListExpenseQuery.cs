using MediatR;
using WebApi.Common.Pagination;
using WebApi.Features.Expenses.DTOs;

namespace WebApi.Features.Expenses.Queries.List;

public sealed record ListExpenseQuery(
     int Page = 1,
     int PageSize = 10,
     string? Category = null,
     string? Search = null,
     string? SortBy = "date",
     string? SortDirection = "desc"
) : IRequest<PagedResponse<ExpenseDto>>;
