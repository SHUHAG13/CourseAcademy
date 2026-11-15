namespace CourseAcademy.Api.Common;

/// <summary>Paging query parameters shared by every list endpoint. Out-of-range values are clamped, not rejected.</summary>
public class PageQuery
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private int _page = 1;
    private int _pageSize = DefaultPageSize;

    public int Page
    {
        get => _page;
        set => _page = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? DefaultPageSize : Math.Min(value, MaxPageSize);
    }

    public int Skip => (Page - 1) * PageSize;
}

/// <summary>The <c>data</c> of a list response: <c>{ items, total, page, pageSize }</c>.</summary>
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int Total, int Page, int PageSize);
