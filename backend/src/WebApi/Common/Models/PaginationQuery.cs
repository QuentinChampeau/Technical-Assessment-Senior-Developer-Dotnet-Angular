namespace WebApi.Common.Pagination;

public sealed class PaginationQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}