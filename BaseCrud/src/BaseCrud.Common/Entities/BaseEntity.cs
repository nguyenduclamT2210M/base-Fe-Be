using System.ComponentModel.DataAnnotations;

namespace BaseCrud.Common.Entities;

/// <summary>Entity gốc: mọi bảng có khóa chính Guid tên Id.</summary>
public abstract class BaseEntity
{
    [Key]
    public Guid Id { get; set; }
}

/// <summary>Bật audit: base tự gán CreatedAt/By khi thêm, UpdatedAt/By khi sửa/xóa mềm.</summary>
public interface IAuditable
{
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
}

/// <summary>Bật xóa mềm: DELETE thành UPDATE IsDeleted = 1, mọi SELECT tự lọc IsDeleted = 0.</summary>
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
}

/// <summary>Bật optimistic concurrency: cột ROWVERSION, UPDATE/DELETE kèm điều kiện RowVersion.</summary>
public interface IHasRowVersion
{
    byte[]? RowVersion { get; set; }
}
