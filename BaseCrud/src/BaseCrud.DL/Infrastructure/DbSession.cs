using System.Data;
using BaseCrud.IDL;
using Microsoft.Data.SqlClient;

namespace BaseCrud.DL.Infrastructure;

/// <summary>
/// 1 connection + (tùy chọn) 1 transaction cho mỗi request (đăng ký Scoped).
/// Connection mở lười (lazy) ở lần đầu cần, đóng khi hết request.
/// </summary>
public sealed class DbSession : IUnitOfWork, IAsyncDisposable, IDisposable
{
    private readonly string _connectionString;
    private SqlConnection? _connection;

    public DbSession(string connectionString) => _connectionString = connectionString;

    public SqlTransaction? Transaction { get; private set; }
    public bool HasTransaction => Transaction is not null;

    public async Task<SqlConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        _connection ??= new SqlConnection(_connectionString);
        if (_connection.State != ConnectionState.Open)
            await _connection.OpenAsync(ct);
        return _connection;
    }

    public async Task BeginAsync(CancellationToken ct = default)
    {
        if (Transaction is not null)
            throw new InvalidOperationException("Transaction đã được mở trong request này.");
        var conn = await GetConnectionAsync(ct);
        Transaction = (SqlTransaction)await conn.BeginTransactionAsync(ct);
    }

    public async Task CommitAsync(CancellationToken ct = default)
    {
        if (Transaction is null) throw new InvalidOperationException("Chưa mở transaction.");
        await Transaction.CommitAsync(ct);
        await Transaction.DisposeAsync();
        Transaction = null;
    }

    public async Task RollbackAsync(CancellationToken ct = default)
    {
        if (Transaction is null) return;
        await Transaction.RollbackAsync(ct);
        await Transaction.DisposeAsync();
        Transaction = null;
    }

    public async ValueTask DisposeAsync()
    {
        // Transaction chưa commit khi dispose = rollback.
        if (Transaction is not null) await Transaction.DisposeAsync();
        if (_connection is not null) await _connection.DisposeAsync();
        Transaction = null;
        _connection = null;
    }

    public void Dispose()
    {
        Transaction?.Dispose();
        _connection?.Dispose();
        Transaction = null;
        _connection = null;
    }
}
