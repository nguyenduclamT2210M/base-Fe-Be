namespace BaseCrud.Common;

/// <summary>Người dùng hiện tại, dùng cho CreatedBy/UpdatedBy. Implement ở tầng API.</summary>
public interface ICurrentUser
{
    string? UserName { get; }
}
