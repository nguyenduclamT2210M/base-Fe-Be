using System.ComponentModel.DataAnnotations.Schema;
using BaseCrud.Common.Validation;

namespace BaseCrud.Common.Entities;

// Lưu ý: KHÔNG import System.ComponentModel.DataAnnotations trong file entity —
// tên Required / MinLength / MaxLength sẽ trùng với luật của BaseCrud.Common.Validation.

/// <summary>Entity mẫu: bật đủ audit + xóa mềm + rowversion.</summary>
[Table("Customer", Schema = "dbo")]
public class Customer : BaseEntity, IAuditable, ISoftDelete, IHasRowVersion
{
    [Label("Mã khách hàng")]
    [Required, MaxLength(20), Code, Unique]
    public string Code { get; set; } = string.Empty;

    [Label("Tên khách hàng")]
    [Required, MaxLength(255)]
    public string Name { get; set; } = string.Empty;

    [Label("Email")]
    [MaxLength(255), Email]
    public string? Email { get; set; }

    // Cột VARCHAR trong DB → khai báo TypeName để parameter là VarChar.
    // Nếu để mặc định (NVarChar) SQL Server sẽ CONVERT_IMPLICIT và bỏ qua index.
    [Label("Số điện thoại")]
    [MaxLength(20), Phone]
    [Column(TypeName = "varchar(20)")]
    public string? Phone { get; set; }

    [Label("Trạng thái")]
    [OneOf]
    public CustomerStatus Status { get; set; } = CustomerStatus.Active;

    [Label("Số dư")]
    [Min(0), Decimals(2)]
    public decimal Balance { get; set; }

    [Label("Ngày sinh")]
    [NotFutureDate, MinAge(18)]
    [Column(TypeName = "date")]
    public DateTime? DateOfBirth { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public byte[]? RowVersion { get; set; }
}

public enum CustomerStatus
{
    Inactive = 0,
    Active = 1,
    Blocked = 2,
}
