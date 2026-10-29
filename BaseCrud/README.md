# BaseCrud — .NET 8 · SQL Server · ADO.NET thuần

Base 3 tầng (Controller → BL → DL) làm sẵn CRUD + query động (filter lồng, sort, paging, chọn cột).

## Cấu trúc

```
src/
├─ BaseCrud.Common   Entity, interface IAuditable/ISoftDelete/IHasRowVersion, QueryRequest, Exception, ApiResponse
├─ BaseCrud.IDL      IBaseDL<T>, IUnitOfWork, ICustomerDL
├─ BaseCrud.DL       BaseDL<T>, DbSession, EntityMetadata, DataReaderMapper, QueryBuilder, CustomerDL
├─ BaseCrud.IBL      IBaseBL<T>, ICustomerBL
├─ BaseCrud.BL       BaseBL<T> (validate, audit, transaction, hook), CustomerBL
└─ BaseCrud.API      BaseController<T>, CustomersController, ExceptionMiddleware, Program.cs
database/001_init.sql
```

Phụ thuộc (mũi tên = "tham chiếu tới"):

```
API ──► IBL ◄── BL ──► IDL ◄── DL
 │                              ▲
 └──────── (chỉ để đăng ký DI) ─┘        Tất cả ──► Common
```

BL **không** biết SqlClient; Controller **không** biết DL. Đổi cách truy cập DB chỉ sửa DL.

## Chạy thử

1. Chạy `database/001_init.sql` trên SQL Server.
2. Sửa `ConnectionStrings:Default` trong `src/BaseCrud.API/appsettings.json`.
   (`TrustServerCertificate=True` cần cho máy local vì SqlClient 4+ bật Encrypt mặc định.)
3. `dotnet run --project src/BaseCrud.API` → mở `http://localhost:5080/swagger`.

## API

| Method | URL | Mô tả |
|---|---|---|
| GET | `/api/customers/{id}` | Lấy 1 bản ghi |
| POST | `/api/customers/query` | Danh sách có filter/sort/paging |
| POST | `/api/customers` | Thêm |
| PUT | `/api/customers/{id}` | Sửa toàn bộ, body phải có `rowVersion` |
| DELETE | `/api/customers/{id}?rowVersion=...` | Xóa (mềm) |
| GET | `/api/customers/by-code/{code}` | Endpoint riêng (ví dụ) |

Ví dụ body `POST /api/customers/query`:

```json
{
  "filter": {
    "logic": "and",
    "conditions": [
      { "field": "status", "op": "in", "value": ["active", "blocked"] },
      { "logic": "or", "conditions": [
        { "field": "name",  "op": "contains",   "value": "nguyễn" },
        { "field": "code",  "op": "startsWith", "value": "KH" }
      ]},
      { "field": "balance", "op": "between", "value": [0, 5000000] }
    ]
  },
  "sort": [{ "field": "name", "dir": "asc" }],
  "page": 1,
  "pageSize": 20,
  "fields": ["code", "name", "status"]
}
```

SQL sinh ra (rút gọn):

```sql
SELECT COUNT_BIG(*) FROM [dbo].[Customer]
WHERE [IsDeleted] = 0 AND (([Status] IN (@p0, @p1) AND ([Name] LIKE @p2 ESCAPE '\' OR [Code] LIKE @p3 ESCAPE '\') AND [Balance] BETWEEN @p4 AND @p5));
SELECT [Id], [RowVersion], [Code], [Name], [Status] FROM [dbo].[Customer] WHERE ... 
ORDER BY [Name] ASC, [Id] ASC
OFFSET @p6 ROWS FETCH NEXT @p7 ROWS ONLY;
```

Operator: `eq, neq, gt, gte, lt, lte, contains, startsWith, endsWith, in, notIn, between, isNull, isNotNull`.

## Thêm 1 bảng mới

**Bảng CRUD thuần (không nghiệp vụ riêng) — 2 file:**

```csharp
// Common/Entities/Product.cs
[Table("Product")]
public class Product : BaseEntity, IAuditable, ISoftDelete, IHasRowVersion { ... }

// API/Controllers/ProductsController.cs
[Route("api/products")]
public class ProductsController(IBaseBL<Product> bl) : BaseController<Product>(bl);
```

`IBaseDL<>` / `IBaseBL<>` đã đăng ký open generic nên không cần đăng ký DI thêm.

**Bảng có nghiệp vụ riêng:** làm như `Customer` — `IXxxDL`, `XxxDL : BaseDL<Xxx>`, `IXxxBL`, `XxxBL : BaseBL<Xxx>`, rồi đăng ký DI trong `Program.cs`. Override hook thay vì viết lại luồng:

| Hook | Dùng cho |
|---|---|
| `Normalize` | Trim, upper-case mã, chuẩn hóa email |
| `ValidateAsync` | Unique, ràng buộc chéo giữa các trường |
| `BeforeSaveAsync` / `AfterSaveAsync` | Ghi bảng phụ, log — chạy trong transaction |
| `BeforeDeleteAsync` | Chặn xóa khi còn dữ liệu phụ thuộc |

## Quy ước entity

| Khai báo | Tác dụng |
|---|---|
| `[Table("X", Schema = "dbo")]` | Tên bảng (mặc định = tên class, schema dbo) |
| `[Column("ten_cot")]` | Tên cột khác tên property |
| `[Column(TypeName = "varchar(20)")]` | Cột VARCHAR/DATE/DATETIME… → parameter đúng kiểu, không mất index |
| `[StringLength(n)]` | Validate + size parameter cố định (tái sử dụng execution plan) |
| `[NotMapped]` | Không phải cột |
| `[NotQueryable]` | Client không được filter/sort/chọn cột này |

## An toàn & giới hạn đã cài

- Giá trị luôn là parameter; tên cột chỉ lấy từ whitelist metadata; operator là enum.
- Filter tối đa 5 cấp lồng, 50 điều kiện, 500 phần tử `in`; `pageSize` tối đa 200.
- Luôn thêm `Id` vào ORDER BY làm tie-breaker → paging ổn định.
- Trường hệ thống (`Id`, `CreatedAt/By`, `IsDeleted`, `RowVersion`) do server gán, bỏ qua giá trị client gửi.
- Unique: BL check trước để báo lỗi rõ; DB unique index là lớp chặn cuối (lỗi 2601/2627 → 409).

## Chưa làm (cố ý, thêm khi thật sự cần)

- **Join / báo cáo phức tạp**: viết SQL riêng trong DL con bằng `QueryAsync<TDto>` (xem `CustomerDL.GetByCodeAsync`) hoặc stored procedure. Đừng cố nhồi join vào base.
- **PATCH (sửa một phần)**: hiện chỉ có PUT (ghi đè toàn bộ cột).
- **Bulk insert**: dùng `SqlBulkCopy` khi cần nhập hàng nghìn dòng.
- **DTO**: API đang nhận/trả entity. Khi entity có cột nhạy cảm hoặc API cần hình dạng khác bảng → thêm DTO + mapping ở BL.
- **Bảng có TRIGGER**: `OUTPUT INSERTED` không có `INTO` sẽ lỗi → override Insert/Update cho bảng đó.
- **COUNT trên bảng rất lớn** có thể chậm → cân nhắc bỏ `total`, chỉ trả `hasNextPage`.
