using System.Collections;
using System.Data;
using System.Globalization;
using System.Text.Json;
using BaseCrud.Common.Exceptions;
using BaseCrud.Common.Helpers;
using BaseCrud.Common.Query;
using BaseCrud.DL.Infrastructure;
using BaseCrud.DL.Metadata;
using Microsoft.Data.SqlClient;

namespace BaseCrud.DL.Query;

/// <summary>
/// Sinh các mảnh SQL (SELECT list, WHERE, ORDER BY) từ QueryRequest.
///
/// 3 nguyên tắc an toàn:
///  1. GIÁ TRỊ luôn là parameter (@p0, @p1...), không bao giờ nối chuỗi.
///  2. TÊN CỘT chỉ lấy từ EntityMetadata (whitelist), đã quote [ ].
///  3. OPERATOR là enum cố định, không nhận chuỗi tự do.
/// Thêm giới hạn độ sâu / số điều kiện / số phần tử IN để chặn request "khủng" làm nghẽn DB.
///
/// Mỗi câu lệnh dùng 1 instance mới (giữ danh sách parameter riêng).
/// </summary>
public sealed class QueryBuilder
{
    public const int MaxPageSize = 200;
    public const int MaxDepth = 5;
    public const int MaxConditions = 50;
    public const int MaxInItems = 500; // SQL Server giới hạn 2100 parameter / lệnh

    private readonly EntityMetadata _meta;
    private readonly List<SqlParameter> _parameters = new();
    private int _paramIndex;
    private int _conditionCount;

    public QueryBuilder(EntityMetadata meta) => _meta = meta;

    public IReadOnlyList<SqlParameter> Parameters => _parameters;

    public string AddParam(object? value, ColumnInfo column)
    {
        var name = NextName();
        _parameters.Add(SqlParam.Create(name, value, column));
        return name;
    }

    public string AddParam(object? value, SqlDbType type)
    {
        var name = NextName();
        _parameters.Add(SqlParam.Create(name, value, type));
        return name;
    }

    private string NextName()
    {
        var name = "@p" + _paramIndex.ToString(CultureInfo.InvariantCulture);
        _paramIndex++;
        return name;
    }

    // ---------------- SELECT ----------------

    public string BuildSelectList(IEnumerable<string>? fields)
    {
        var requested = fields?.Where(f => !string.IsNullOrWhiteSpace(f)).ToList();
        if (requested is null || requested.Count == 0) return _meta.SelectList;

        // Luôn trả Id (và RowVersion) để client còn sửa/xóa được bản ghi
        var cols = new List<ColumnInfo> { _meta.Key };
        if (_meta.RowVersion is not null) cols.Add(_meta.RowVersion);
        foreach (var f in requested)
        {
            var col = _meta.GetQueryableOrThrow(f);
            if (!cols.Contains(col)) cols.Add(col);
        }
        return string.Join(", ", cols.Select(c => c.QuotedName));
    }

    // ---------------- WHERE ----------------

    /// <param name="filter">Filter từ client (có thể null).</param>
    /// <param name="extraConditions">Điều kiện do server tự sinh (đã parameter hóa), luôn AND.</param>
    public string BuildWhere(FilterNode? filter, IEnumerable<string>? extraConditions = null)
    {
        var parts = new List<string>();

        if (_meta.SoftDeleteColumn is { } sd)
            parts.Add($"{sd.QuotedName} = 0");

        if (extraConditions is not null)
            parts.AddRange(extraConditions);

        if (filter is not null)
        {
            var f = BuildNode(filter, 0);
            if (f.Length > 0) parts.Add($"({f})");
        }

        return parts.Count == 0 ? string.Empty : "WHERE " + string.Join(" AND ", parts);
    }

    private string BuildNode(FilterNode node, int depth)
    {
        if (depth > MaxDepth)
            throw new ValidateException("filter", $"Filter lồng quá {MaxDepth} cấp.");

        if (node.IsGroup)
        {
            var children = node.Conditions!
                .Select(c => BuildNode(c, depth + 1))
                .Where(s => s.Length > 0)
                .ToList();
            if (children.Count == 0) return string.Empty;
            if (children.Count == 1) return children[0];
            var logic = node.Logic == FilterLogic.Or ? " OR " : " AND ";
            return "(" + string.Join(logic, children) + ")";
        }

        if (++_conditionCount > MaxConditions)
            throw new ValidateException("filter", $"Tối đa {MaxConditions} điều kiện.");
        if (string.IsNullOrWhiteSpace(node.Field))
            throw new ValidateException("filter", "Điều kiện thiếu 'field'.");

        var col = _meta.GetQueryableOrThrow(node.Field);
        var c = col.QuotedName;

        switch (node.Op)
        {
            case FilterOperator.IsNull:
                return $"{c} IS NULL";

            case FilterOperator.IsNotNull:
                return $"{c} IS NOT NULL";

            case FilterOperator.Eq:
            {
                var v = ConvertValue(node.Value, col);
                return v is null ? $"{c} IS NULL" : $"{c} = {AddParam(v, col)}";
            }

            case FilterOperator.Neq:
            {
                var v = ConvertValue(node.Value, col);
                if (v is null) return $"{c} IS NOT NULL";
                // NULL <> x cho ra UNKNOWN → dòng NULL bị loại. Đa số người dùng mong nhận cả dòng NULL.
                return col.IsNullable
                    ? $"({c} <> {AddParam(v, col)} OR {c} IS NULL)"
                    : $"{c} <> {AddParam(v, col)}";
            }

            case FilterOperator.Gt:
            case FilterOperator.Gte:
            case FilterOperator.Lt:
            case FilterOperator.Lte:
            {
                var v = RequireValue(node, col);
                var op = node.Op switch
                {
                    FilterOperator.Gt => ">",
                    FilterOperator.Gte => ">=",
                    FilterOperator.Lt => "<",
                    _ => "<=",
                };
                return $"{c} {op} {AddParam(v, col)}";
            }

            case FilterOperator.Contains:
            case FilterOperator.StartsWith:
            case FilterOperator.EndsWith:
            {
                if (col.ClrType != typeof(string))
                    throw new ValidateException(col.PropertyName, $"'{col.PropertyName}' không phải kiểu chuỗi, không dùng được {node.Op}.");
                var s = EscapeLike((string)RequireValue(node, col));
                var pattern = node.Op switch
                {
                    FilterOperator.Contains => $"%{s}%",
                    FilterOperator.StartsWith => $"{s}%",
                    _ => $"%{s}",
                };
                return $"{c} LIKE {AddParam(pattern, col)} ESCAPE '\\'";
            }

            case FilterOperator.In:
            case FilterOperator.NotIn:
            {
                var items = ConvertList(node.Value, col);
                if (items.Count == 0) return node.Op == FilterOperator.In ? "1 = 0" : "1 = 1";
                if (items.Count > MaxInItems)
                    throw new ValidateException(col.PropertyName, $"Tối đa {MaxInItems} giá trị cho {node.Op}.");
                var names = string.Join(", ", items.Select(i => AddParam(i, col)));
                return node.Op == FilterOperator.In ? $"{c} IN ({names})" : $"{c} NOT IN ({names})";
            }

            case FilterOperator.Between:
            {
                var items = ConvertList(node.Value, col);
                if (items.Count != 2)
                    throw new ValidateException(col.PropertyName, "Between cần đúng 2 giá trị [from, to].");
                return $"{c} BETWEEN {AddParam(items[0], col)} AND {AddParam(items[1], col)}";
            }

            default:
                throw new ValidateException("filter", $"Operator '{node.Op}' không được hỗ trợ.");
        }
    }

    // ---------------- ORDER BY ----------------

    public string BuildOrderBy(IEnumerable<SortItem>? sort)
    {
        var items = new List<string>();
        var used = new HashSet<ColumnInfo>();

        foreach (var s in sort ?? Enumerable.Empty<SortItem>())
        {
            var col = _meta.GetQueryableOrThrow(s.Field);
            if (!used.Add(col)) continue;
            items.Add($"{col.QuotedName} {(s.Dir == SortDirection.Desc ? "DESC" : "ASC")}");
        }

        if (items.Count == 0 && _meta.DefaultSortColumn is { } d)
        {
            items.Add($"{d.QuotedName} DESC");
            used.Add(d);
        }

        // Tie-breaker: sort theo cột không unique (vd Status) mà không có Id
        // → thứ tự giữa các trang không ổn định, cùng 1 dòng có thể xuất hiện ở 2 trang.
        if (!used.Contains(_meta.Key))
            items.Add($"{_meta.Key.QuotedName} ASC");

        return "ORDER BY " + string.Join(", ", items);
    }

    // ---------------- Convert value ----------------

    private static object RequireValue(FilterNode node, ColumnInfo col)
        => ConvertValue(node.Value, col)
           ?? throw new ValidateException(col.PropertyName, $"Operator {node.Op} của '{col.PropertyName}' cần 'value'.");

    /// <summary>Ép value (JsonElement hoặc object) về đúng kiểu CLR của cột. Sai kiểu → 400.</summary>
    private static object? ConvertValue(object? raw, ColumnInfo col)
    {
        if (raw is null) return null;
        var target = col.ClrType;
        try
        {
            if (raw is JsonElement je)
            {
                if (je.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined) return null;
                return je.Deserialize(target, JsonDefaults.Options);
            }
            if (target.IsInstanceOfType(raw)) return raw;
            if (target.IsEnum) return raw is string es ? Enum.Parse(target, es, true) : Enum.ToObject(target, raw);
            if (target == typeof(Guid)) return raw is string gs ? Guid.Parse(gs) : raw;
            return Convert.ChangeType(raw, target, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is JsonException or FormatException or InvalidCastException
                                       or OverflowException or ArgumentException or NotSupportedException)
        {
            throw new ValidateException(col.PropertyName, $"Giá trị không hợp lệ cho '{col.PropertyName}'.");
        }
    }

    private static List<object> ConvertList(object? raw, ColumnInfo col)
    {
        IEnumerable<object?> items = raw switch
        {
            JsonElement { ValueKind: JsonValueKind.Array } je => je.EnumerateArray().Select(e => (object?)e),
            IEnumerable e and not string => e.Cast<object?>(),
            _ => throw new ValidateException(col.PropertyName, $"'{col.PropertyName}': value phải là mảng."),
        };

        var list = new List<object>();
        foreach (var item in items)
        {
            var v = ConvertValue(item, col);
            if (v is not null) list.Add(v);
        }
        return list;
    }

    /// <summary>Escape ký tự đặc biệt của LIKE để "50%" được hiểu là chuỗi "50%", không phải wildcard.</summary>
    private static string EscapeLike(string s)
        => s.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_").Replace("[", "\\[");
}
