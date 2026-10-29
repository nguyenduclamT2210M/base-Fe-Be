using BaseCrud.Common.Entities;

namespace BaseCrud.IDL;

public interface ICustomerDL : IBaseDL<Customer>
{
    Task<Customer?> GetByCodeAsync(string code, CancellationToken ct = default);
}
