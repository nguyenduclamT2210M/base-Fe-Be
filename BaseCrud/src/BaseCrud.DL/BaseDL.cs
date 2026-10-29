using System.Data;
using BaseCrud.Common.Entities;
using BaseCrud.Common.Query;
using BaseCrud.DL.Infrastructure;
using BaseCrud.DL.Mapping;
using BaseCrud.DL.Metadata;
using BaseCrud.DL.Query;
using BaseCrud.IDL;
using Microsoft.Data.SqlClient;

namespace BaseCrud.DL;

/// <summary>
/// CRUD chung cho mọi entity. Không abstract: entity đơn giản dùng thẳng BaseDL&lt;T&gt;
/// (đăng ký open generic), entity có nghiệp vụ riêng thì kế thừa và override / thêm method.
///
/// Lưu ý OUTPUT INSERTED (không có INTO) sẽ lỗi nếu bảng có TRIGGER.
/// Bảng có trigger → override InsertAsync/UpdateAsync, dùng OUTPUT ... INTO @table hoặc SELECT lại.
/// </summary>
public class BaseDL<T> : IBaseDL<T> where T : BaseEntity, new()
{
    protected static readonly EntityMetadata Meta = EntityMetadata.For<T>();
    protected readonly DbSession Session;

    public BaseDL(DbSession session) => Session = session;

    // ===================== CRUD =====================

    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var qb = new QueryBuilder(Meta);
        var where = qb.BuildWhere(null, new[] { $"{Meta.Key.QuotedName} = {qb.AddParam(id, Meta.Key)}" });
        var sql = $"SELECT {Meta.SelectList} FROM {Meta.TableName} {where};";
        var rows = await QueryAsync<T>(sql, qb.Parameters, ct);
        return rows.FirstOrDefault();
    }

    public virtual async Task<PagedResult<T>> GetPagedAsync(QueryRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var page = Math.Max(1, request.Page);
        var size = Math.Clamp(request.PageSize, 1, QueryBuilder.MaxPageSize);

        var qb = new QueryBuilder(Meta);
        var select = qb.BuildSelectList(request.Fields);
        var where = qb.BuildWhere(request.Filter);
        var orderBy = qb.BuildOrderBy(request.Sort);
        var offsetP = qb.AddParam((long)(page - 1) * size, SqlDbType.BigInt);
        var sizeP = qb.AddParam(size, SqlDbType.Int);

        // 2 câu trong 1 batch → 1 round-trip. COUNT trên bảng hàng triệu dòng có thể chậm,
        // khi đó cân nhắc bỏ Total (chỉ trả hasNextPage) hoặc cache.
        var sql = $"""
            SELECT COUNT_BIG(*) FROM {Meta.TableName} {where};
            SELECT {select} FROM {Meta.TableName} {where}
            {orderBy}
            OFFSET {offsetP} ROWS FETCH NEXT {sizeP} ROWS ONLY;
            """;

        await using var cmd = await CreateCommandAsync(sql, qb.Parameters, ct);
        await using var reader = await cmd.ExecuteReaderAsync(ct);

        long total = 0;
        if (await reader.ReadAsync(ct)) total = reader.GetInt64(0);
        await reader.NextResultAsync(ct);
        var items = await DataReaderMapper.ReadAllAsync<T>(reader, Meta, ct);

        return new PagedResult<T> { Items = items, Total = total, Page = page, PageSize = size };
    }

    public virtual async Task<T> InsertAsync(T entity, CancellationToken ct = default)
    {
        var qb = new QueryBuilder(Meta);
        var cols = Meta.Columns.Where(c => !c.IsRowVersion).ToList(); // ROWVERSION do DB tự sinh
        var names = string.Join(", ", cols.Select(c => c.QuotedName));
        var values = string.Join(", ", cols.Select(c => qb.AddParam(c.Getter(entity), c)));

        var sql = $"INSERT INTO {Meta.TableName} ({names}) OUTPUT {Meta.OutputList} VALUES ({values});";
        var rows = await QueryAsync<T>(sql, qb.Parameters, ct);
        return rows.Single();
    }

    public virtual async Task<T?> UpdateAsync(T entity, CancellationToken ct = default)
    {
        var qb = new QueryBuilder(Meta);
        var sets = string.Join(", ", Meta.Columns
            .Where(c => c.IsUpdatable)
            .Select(c => $"{c.QuotedName} = {qb.AddParam(c.Getter(entity), c)}"));

        var conds = new List<string> { $"{Meta.Key.QuotedName} = {qb.AddParam(entity.Id, Meta.Key)}" };
        if (Meta.RowVersion is { } rv && entity is IHasRowVersion h)
            conds.Add($"{rv.QuotedName} = {qb.AddParam(h.RowVersion, rv)}");

        var where = qb.BuildWhere(null, conds);
        var sql = $"UPDATE {Meta.TableName} SET {sets} OUTPUT {Meta.OutputList} {where};";
        var rows = await QueryAsync<T>(sql, qb.Parameters, ct);
        return rows.FirstOrDefault();
    }

    public virtual async Task<int> DeleteAsync(Guid id, byte[]? rowVersion, string? actor, CancellationToken ct = default)
    {
        var qb = new QueryBuilder(Meta);
        var conds = new List<string> { $"{Meta.Key.QuotedName} = {qb.AddParam(id, Meta.Key)}" };
        if (Meta.RowVersion is { } rv && rowVersion is not null)
            conds.Add($"{rv.QuotedName} = {qb.AddParam(rowVersion, rv)}");
        var where = qb.BuildWhere(null, conds);

        string sql;
        if (Meta.SoftDeleteColumn is { } sd)
        {
            var sets = new List<string> { $"{sd.QuotedName} = 1" };
            if (Meta.IsAuditable)
            {
                var updatedAt = Meta.GetColumnOrThrow(nameof(IAuditable.UpdatedAt));
                var updatedBy = Meta.GetColumnOrThrow(nameof(IAuditable.UpdatedBy));
                sets.Add($"{updatedAt.QuotedName} = SYSUTCDATETIME()");
                sets.Add($"{updatedBy.QuotedName} = {qb.AddParam(actor, updatedBy)}");
            }
            sql = $"UPDATE {Meta.TableName} SET {string.Join(", ", sets)} {where};";
        }
        else
        {
            sql = $"DELETE FROM {Meta.TableName} {where};";
        }

        return await ExecuteAsync(sql, qb.Parameters, ct);
    }

    public virtual async Task<bool> ExistsAsync(Guid id, CancellationToken ct = default)
    {
        var qb = new QueryBuilder(Meta);
        var where = qb.BuildWhere(null, new[] { $"{Meta.Key.QuotedName} = {qb.AddParam(id, Meta.Key)}" });
        var sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {Meta.TableName} {where}) THEN 1 ELSE 0 END;";
        return Convert.ToInt32(await ScalarAsync(sql, qb.Parameters, ct)) == 1;
    }

    public virtual async Task<bool> IsValueTakenAsync(string propertyName, object? value, Guid? excludeId = null,
        CancellationToken ct = default)
    {
        var col = Meta.GetColumnOrThrow(propertyName);
        var qb = new QueryBuilder(Meta);
        var conds = new List<string>
        {
            value is null ? $"{col.QuotedName} IS NULL" : $"{col.QuotedName} = {qb.AddParam(value, col)}",
        };
        if (excludeId is Guid ex)
            conds.Add($"{Meta.Key.QuotedName} <> {qb.AddParam(ex, Meta.Key)}");

        var where = qb.BuildWhere(null, conds); // tự thêm IsDeleted = 0 → khớp filtered unique index
        var sql = $"SELECT CASE WHEN EXISTS (SELECT 1 FROM {Meta.TableName} {where}) THEN 1 ELSE 0 END;";
        return Convert.ToInt32(await ScalarAsync(sql, qb.Parameters, ct)) == 1;
    }

    // ============ Helper cho class con tự viết SQL ("lối thoát") ============

    protected async Task<SqlCommand> CreateCommandAsync(string sql, IEnumerable<SqlParameter>? parameters,
        CancellationToken ct, CommandType commandType = CommandType.Text)
    {
        var conn = await Session.GetConnectionAsync(ct);
        var cmd = conn.CreateCommand();
        cmd.CommandText = sql;
        cmd.CommandType = commandType;
        cmd.Transaction = Session.Transaction;
        if (parameters is not null)
            foreach (var p in parameters) cmd.Parameters.Add(p);
        return cmd;
    }

    /// <summary>Chạy SELECT và map kết quả sang TResult (entity hoặc DTO bất kỳ có property trùng tên cột).</summary>
    protected async Task<List<TResult>> QueryAsync<TResult>(string sql, IEnumerable<SqlParameter>? parameters,
        CancellationToken ct, CommandType commandType = CommandType.Text) where TResult : new()
    {
        await using var cmd = await CreateCommandAsync(sql, parameters, ct, commandType);
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        return await DataReaderMapper.ReadAllAsync<TResult>(reader, EntityMetadata.For<TResult>(), ct);
    }

    protected async Task<int> ExecuteAsync(string sql, IEnumerable<SqlParameter>? parameters,
        CancellationToken ct, CommandType commandType = CommandType.Text)
    {
        await using var cmd = await CreateCommandAsync(sql, parameters, ct, commandType);
        return await cmd.ExecuteNonQueryAsync(ct);
    }

    protected async Task<object?> ScalarAsync(string sql, IEnumerable<SqlParameter>? parameters,
        CancellationToken ct, CommandType commandType = CommandType.Text)
    {
        await using var cmd = await CreateCommandAsync(sql, parameters, ct, commandType);
        var result = await cmd.ExecuteScalarAsync(ct);
        return result is DBNull ? null : result;
    }
}
