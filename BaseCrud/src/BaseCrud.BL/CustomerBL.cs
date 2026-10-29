using BaseCrud.Common;
using BaseCrud.Common.Entities;
using BaseCrud.Common.Exceptions;
using BaseCrud.IBL;
using BaseCrud.IDL;

namespace BaseCrud.BL;

/// <summary>
/// Validate định dạng + trùng mã đã khai báo bằng attribute trên Customer.
/// Ở đây chỉ còn chuẩn hóa dữ liệu và luật nghiệp vụ đặc thù.
/// </summary>
public class CustomerBL : BaseBL<Customer>, ICustomerBL
{
    private readonly ICustomerDL _customerDL;

    public CustomerBL(ICustomerDL dl, IUnitOfWork unitOfWork, ICurrentUser currentUser)
        : base(dl, unitOfWork, currentUser) => _customerDL = dl;

    public async Task<Customer> GetByCodeAsync(string code, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ValidateException("code", "Mã khách hàng không được để trống");
        return await _customerDL.GetByCodeAsync(code.Trim().ToUpperInvariant(), ct)
               ?? throw new NotFoundException($"Không tìm thấy khách hàng có mã “{code}”.");
    }

    protected override void Normalize(Customer e)
    {
        e.Code = e.Code?.Trim().ToUpperInvariant() ?? string.Empty;
        e.Name = e.Name?.Trim() ?? string.Empty;
        e.Email = string.IsNullOrWhiteSpace(e.Email) ? null : e.Email.Trim().ToLowerInvariant();
        e.Phone = string.IsNullOrWhiteSpace(e.Phone) ? null : e.Phone.Trim();
    }

    protected override Task ValidateAsync(Customer e, bool isInsert,
        IDictionary<string, List<string>> errors, CancellationToken ct)
    {
        // Ví dụ luật nghiệp vụ: khách bị khóa thì không được có số dư
        if (e.Status == CustomerStatus.Blocked && e.Balance > 0)
            AddError(errors, nameof(Customer.Balance), "Khách hàng bị khóa phải có số dư bằng 0");
        return Task.CompletedTask;
    }
}
