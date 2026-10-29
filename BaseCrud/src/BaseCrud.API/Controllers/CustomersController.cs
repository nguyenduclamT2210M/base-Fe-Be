using BaseCrud.Common.Entities;
using BaseCrud.Common.Responses;
using BaseCrud.IBL;
using Microsoft.AspNetCore.Mvc;

namespace BaseCrud.API.Controllers;

[Route("api/customers")]
public class CustomersController : BaseController<Customer>
{
    private readonly ICustomerBL _customerBL;

    public CustomersController(ICustomerBL bl) : base(bl) => _customerBL = bl;

    /// <summary>Ví dụ endpoint riêng ngoài 5 endpoint CRUD.</summary>
    [HttpGet("by-code/{code}")]
    public async Task<ActionResult<ApiResponse<Customer>>> GetByCode(string code, CancellationToken ct)
        => Ok(ApiResponse<Customer>.Ok(await _customerBL.GetByCodeAsync(code, ct)));
}
