using System.Text.Json.Serialization;

namespace BaseCrud.Common.Query;

public enum FilterLogic { And, Or }

public enum FilterOperator
{
    Eq, Neq, Gt, Gte, Lt, Lte,
    Contains, StartsWith, EndsWith,
    In, NotIn, Between,
    IsNull, IsNotNull,
}

public enum SortDirection { Asc, Desc }

/// <summary>
/// Một node của cây filter.
/// - Nhóm: có Conditions (+ Logic and/or), các node con có thể lồng tiếp.
/// - Lá: có Field + Op + Value.
/// </summary>
public class FilterNode
{
    public FilterLogic Logic { get; set; } = FilterLogic.And;
    public List<FilterNode>? Conditions { get; set; }

    public string? Field { get; set; }
    public FilterOperator Op { get; set; } = FilterOperator.Eq;

    /// <summary>Từ JSON sẽ là JsonElement; base tự convert sang đúng kiểu của cột.</summary>
    public object? Value { get; set; }

    [JsonIgnore]
    public bool IsGroup => Conditions is not null;
}

public class SortItem
{
    public string Field { get; set; } = string.Empty;
    public SortDirection Dir { get; set; } = SortDirection.Asc;
}

public class QueryRequest
{
    public FilterNode? Filter { get; set; }
    public List<SortItem>? Sort { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;

    /// <summary>Chọn cột trả về (tên property). Rỗng = tất cả. Id và RowVersion luôn có.</summary>
    public List<string>? Fields { get; set; }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = Array.Empty<T>();
    public long Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);
}
