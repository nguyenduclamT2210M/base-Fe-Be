using BaseCrud.API.Infrastructure;
using BaseCrud.BL;
using BaseCrud.Common;
using BaseCrud.Common.Helpers;
using BaseCrud.Common.Responses;
using BaseCrud.DL;
using BaseCrud.DL.Infrastructure;
using BaseCrud.IBL;
using BaseCrud.IDL;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// ---------- MVC + JSON ----------
builder.Services.AddControllers(o =>
    {
        // Mặc định ASP.NET tự coi mọi property `string` (không có ?) là [Required] và trả câu lỗi tiếng Anh
        // "The Code field is required." TRƯỚC khi tới BL. Tắt đi để mọi luật nằm ở một chỗ: attribute + BL.
        o.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    })
    .AddJsonOptions(o => JsonDefaults.Apply(o.JsonSerializerOptions))
    .ConfigureApiBehaviorOptions(o =>
    {
        // Lỗi đọc request (JSON sai kiểu, ngày 31/02...) → cùng format ApiResponse, câu lỗi tiếng Việt
        o.InvalidModelStateResponseFactory = ctx =>
            new BadRequestObjectResult(ApiResponse<object>.Fail(
                "Dữ liệu không hợp lệ.", ModelStateTranslator.Translate(ctx)));
    });

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();

// ---------- Data ----------
var connectionString = builder.Configuration.GetConnectionString("Default")
                       ?? throw new InvalidOperationException("Thiếu ConnectionStrings:Default trong appsettings.");

// 1 DbSession / request; IUnitOfWork trỏ cùng instance để BL và DL dùng chung connection + transaction
builder.Services.AddScoped(_ => new DbSession(connectionString));
builder.Services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<DbSession>());

// Open generic: entity CRUD thuần không cần viết DL/BL riêng
builder.Services.AddScoped(typeof(IBaseDL<>), typeof(BaseDL<>));
builder.Services.AddScoped(typeof(IBaseBL<>), typeof(BaseBL<>));

// Entity có nghiệp vụ riêng
builder.Services.AddScoped<ICustomerDL, CustomerDL>();
builder.Services.AddScoped<ICustomerBL, CustomerBL>();

var app = builder.Build();

app.UseMiddleware<ExceptionMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.Run();
