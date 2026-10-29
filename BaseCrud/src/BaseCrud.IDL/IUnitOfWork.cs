namespace BaseCrud.IDL;

/// <summary>
/// Quản lý transaction trong phạm vi 1 request.
/// Mọi DL trong cùng request dùng chung connection + transaction này.
/// </summary>
public interface IUnitOfWork
{
    bool HasTransaction { get; }
    Task BeginAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
