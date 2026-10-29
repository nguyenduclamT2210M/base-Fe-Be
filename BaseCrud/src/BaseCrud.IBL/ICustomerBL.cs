using BaseCrud.Common.Entities;

namespace BaseCrud.IBL;

public interface ICustomerBL : IBaseBL<Customer>
{
    Task<Customer> GetByCodeAsync(string code, CancellationToken ct = default);
}
