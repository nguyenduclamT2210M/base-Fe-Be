using BaseCrud.Common.Entities;
using BaseCrud.DL.Infrastructure;
using BaseCrud.IDL;

namespace BaseCrud.DL;

public class CustomerDL : BaseDL<Customer>, ICustomerDL
{
    public CustomerDL(DbSession session) : base(session) { }

    /// <summary>
    /// Ví dụ "lối thoát": nghiệp vụ base không phủ → tự viết SQL.
    /// Vẫn tuân thủ: tên cột lấy từ metadata, giá trị qua parameter.
    /// </summary>
    public async Task<Customer?> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        var codeCol = Meta.GetColumnOrThrow(nameof(Customer.Code));
        var sql = $"""
            SELECT {Meta.SelectList}
            FROM {Meta.TableName}
            WHERE {codeCol.QuotedName} = @code AND [IsDeleted] = 0;
            """;
        var rows = await QueryAsync<Customer>(sql, new[] { SqlParam.Create("@code", code, codeCol) }, ct);
        return rows.FirstOrDefault();
    }
}
