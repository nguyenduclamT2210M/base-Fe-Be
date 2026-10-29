using BaseCrud.Common.Validation;

namespace BaseCrud.API.Models;

/// <summary>Form mẫu dùng ĐỦ các luật — để thử trên Swagger và làm mẫu copy khi tạo DTO thật.</summary>
public class EmployeeForm
{
    // ---- Bắt buộc ----
    [Label("Họ tên")]
    [Required, LengthBetween(2, 50), PersonName]
    public string? FullName { get; set; }

    [Label("Phòng ban")]
    [RequiredSelect]
    public Guid? DepartmentId { get; set; }

    [Label("Điều khoản sử dụng")]
    [Accepted]
    public bool AgreeTerms { get; set; }

    // ---- Định dạng ----
    [Label("Mã NV")]
    [Required, ExactLength(5), Code]
    public string? EmployeeCode { get; set; }

    [Label("Tên đăng nhập")]
    [Required, MinLength(4), MaxLength(30), NoWhitespace]
    public string? Username { get; set; }

    [Label("Email")]
    [Required, Email]
    public string? Email { get; set; }

    [Label("Số điện thoại")]
    [Required, Phone]
    public string? Phone { get; set; }

    [Label("Website")]
    [Url]
    public string? Website { get; set; }

    [Label("Mã số thuế")]
    [Pattern(@"^\d{10}(\d{3})?$", Message = "{label} phải gồm 10 hoặc 13 chữ số")]
    public string? TaxCode { get; set; }

    // ---- Mật khẩu ----
    [Label("Mật khẩu")]
    [Required, Password]
    public string? Password { get; set; }

    [Label("Xác nhận mật khẩu")]
    [SameAs(nameof(Password))]
    public string? ConfirmPassword { get; set; }

    // ---- Số ----
    [Label("Lương")]
    [Required, Between(1_000_000, 500_000_000)]
    public decimal? Salary { get; set; }

    [Label("Phụ cấp")]
    [Min(0), Decimals(2)]
    public decimal? Allowance { get; set; }

    [Label("Số con")]
    [Min(0), Max(20)]
    public int? Children { get; set; }

    [Label("Số năm kinh nghiệm")]
    [Number, Integer, Between(0, 50)]
    public string? ExperienceYears { get; set; }

    // ---- Ngày ----
    [Label("Ngày sinh")]
    [Required, NotFutureDate, MinAge(18), MaxAge(65)]
    public DateTime? DateOfBirth { get; set; }

    [Label("Ngày bắt đầu")]
    [NotPastDate]
    public DateTime? StartDate { get; set; }

    [Label("Ngày kết thúc")]
    [DateAfter(nameof(StartDate))]
    public DateTime? EndDate { get; set; }

    [Label("Ngày cấp CCCD")]
    [Date, NotFutureDate]
    public string? IdIssuedDate { get; set; }

    // ---- Danh sách ----
    [Label("Giới tính")]
    [Required, OneOf("male", "female", "other")]
    public string? Gender { get; set; }

    [Label("Kỹ năng")]
    [MaxItems(5)]
    public List<string>? Skills { get; set; }
}

/// <summary>Form upload (multipart/form-data) — luật tệp.</summary>
public class UploadForm
{
    [Label("Ảnh đại diện")]
    [Required, FileSize(2), FileType("jpg", "jpeg", "png")]
    public IFormFile? Avatar { get; set; }

    [Label("Tài liệu đính kèm")]
    [MaxItems(3), FileSize(5), FileType("pdf", "docx", "xlsx")]
    public List<IFormFile>? Documents { get; set; }
}
