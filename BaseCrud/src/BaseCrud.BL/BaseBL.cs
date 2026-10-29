using System.ComponentModel;
using System.Text.Json;
using BaseCrud.Common;
using BaseCrud.Common.Entities;
using BaseCrud.Common.Exceptions;
using BaseCrud.Common.Helpers;
using BaseCrud.Common.Query;
using BaseCrud.Common.Validation;
using BaseCrud.IBL;
using BaseCrud.IDL;

namespace BaseCrud.BL;

/// <summary>
/// Luồng chung:
///   Insert: Normalize → gán Id/audit → Validate → [Tx] BeforeSave → DL.Insert → AfterSave
///   Update: Normalize → gán audit → Validate → [Tx] BeforeSave → DL.Update → AfterSave
///   Delete: [Tx] BeforeDelete → DL.Delete
/// Class con override các hook thay vì viết lại cả luồng.
/// </summary>
public class BaseBL<T> : IBaseBL<T> where T : BaseEntity, new()
{
    protected readonly IBaseDL<T> DL;
    protected readonly IUnitOfWork UnitOfWork;
    protected readonly ICurrentUser CurrentUser;

    protected static string EntityName => typeof(T).Name;

    public BaseBL(IBaseDL<T> dl, IUnitOfWork unitOfWork, ICurrentUser currentUser)
    {
        DL = dl;
        UnitOfWork = unitOfWork;
        CurrentUser = currentUser;
    }

    public virtual async Task<T> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await DL.GetByIdAsync(id, ct) ?? throw new NotFoundException(EntityName, id);

    public virtual Task<PagedResult<T>> GetPagedAsync(QueryRequest request, CancellationToken ct = default)
        => DL.GetPagedAsync(request ?? new QueryRequest(), ct);

    public virtual async Task<T> InsertAsync(T entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Normalize(entity);

        // Các trường hệ thống luôn do server gán, bỏ qua giá trị client gửi (chống over-posting)
        entity.Id = SequentialGuid.NewGuid();
        if (entity is IAuditable a)
        {
            a.CreatedAt = DateTime.UtcNow;
            a.CreatedBy = CurrentUser.UserName;
            a.UpdatedAt = null;
            a.UpdatedBy = null;
        }
        if (entity is ISoftDelete sd) sd.IsDeleted = false;
        if (entity is IHasRowVersion rv) rv.RowVersion = null;

        await ValidateOrThrowAsync(entity, isInsert: true, ct);

        return await RunInTransactionAsync(async () =>
        {
            await BeforeSaveAsync(entity, isInsert: true, ct);
            var saved = await DL.InsertAsync(entity, ct);
            await AfterSaveAsync(saved, isInsert: true, ct);
            return saved;
        }, ct);
    }

    public virtual async Task<T> UpdateAsync(Guid id, T entity, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        Normalize(entity);

        entity.Id = id; // Id lấy từ URL, không tin Id trong body
        if (entity is IAuditable a)
        {
            a.UpdatedAt = DateTime.UtcNow;
            a.UpdatedBy = CurrentUser.UserName;
        }
        if (entity is IHasRowVersion { RowVersion: null })
            throw new ValidateException("rowVersion", "Thiếu rowVersion (lấy từ lần GET gần nhất).");

        await ValidateOrThrowAsync(entity, isInsert: false, ct);

        return await RunInTransactionAsync(async () =>
        {
            await BeforeSaveAsync(entity, isInsert: false, ct);
            var saved = await DL.UpdateAsync(entity, ct);
            if (saved is null) throw await NotFoundOrConcurrencyAsync(id, ct);
            await AfterSaveAsync(saved, isInsert: false, ct);
            return saved;
        }, ct);
    }

    public virtual async Task DeleteAsync(Guid id, byte[]? rowVersion, CancellationToken ct = default)
    {
        await RunInTransactionAsync(async () =>
        {
            await BeforeDeleteAsync(id, ct);
            var affected = await DL.DeleteAsync(id, rowVersion, CurrentUser.UserName, ct);
            if (affected == 0) throw await NotFoundOrConcurrencyAsync(id, ct);
            return true;
        }, ct);
    }

    // ===================== Hook cho class con =====================

    /// <summary>Chuẩn hóa dữ liệu trước khi validate (trim, upper-case mã...).</summary>
    protected virtual void Normalize(T entity) { }

    /// <summary>Validate nghiệp vụ không diễn đạt được bằng attribute. Luật attribute + [Unique] đã chạy trước.</summary>
    protected virtual Task ValidateAsync(T entity, bool isInsert, IDictionary<string, List<string>> errors,
        CancellationToken ct) => Task.CompletedTask;

    protected virtual Task BeforeSaveAsync(T entity, bool isInsert, CancellationToken ct) => Task.CompletedTask;
    protected virtual Task AfterSaveAsync(T entity, bool isInsert, CancellationToken ct) => Task.CompletedTask;
    protected virtual Task BeforeDeleteAsync(Guid id, CancellationToken ct) => Task.CompletedTask;

    // ===================== Helper =====================

    /// <summary>
    /// Thứ tự: luật attribute → [Unique] (gọi DB, chỉ với trường chưa lỗi) → ValidateAsync của class con.
    /// Validate ở BL (không ở Controller) vì BL có thể được gọi từ nơi khác (job, import Excel...).
    /// </summary>
    protected async Task ValidateOrThrowAsync(T entity, bool isInsert, CancellationToken ct)
    {
        var errors = RuleValidator.Validate(entity).ToDictionary(e => e.Key, e => e.Value.ToList());

        foreach (var u in RuleValidator.GetUniqueFields(typeof(T)))
        {
            var key = JsonNamingPolicy.CamelCase.ConvertName(u.Property.Name);
            if (errors.ContainsKey(key)) continue; // đã sai định dạng thì khỏi gọi DB
            var value = u.GetValue(entity);
            if (RuleFormat.IsEmpty(value)) continue;
            if (await DL.IsValueTakenAsync(u.Property.Name, value, isInsert ? null : entity.Id, ct))
                AddError(errors, u.Property.Name, u.FormatMessage(value));
        }

        await ValidateAsync(entity, isInsert, errors, ct);

        if (errors.Count > 0)
            throw new ValidateException(errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
    }

    public virtual async Task<UniqueCheckResult> CheckUniqueAsync(string field, string? value, Guid? excludeId,
        CancellationToken ct = default)
    {
        // Chỉ cho kiểm tra trường có [Unique] → không ai dùng endpoint này để dò dữ liệu trường khác
        var u = RuleValidator.GetUniqueFields(typeof(T))
                    .FirstOrDefault(x => string.Equals(x.Property.Name, field, StringComparison.OrdinalIgnoreCase))
                ?? throw new ValidateException("field", $"Trường '{field}' không hỗ trợ kiểm tra trùng.");

        var text = value?.Trim();
        if (string.IsNullOrEmpty(text)) return new UniqueCheckResult { IsTaken = false };

        object typed;
        try
        {
            var type = Nullable.GetUnderlyingType(u.Property.PropertyType) ?? u.Property.PropertyType;
            typed = type == typeof(string)
                ? text
                : TypeDescriptor.GetConverter(type).ConvertFromInvariantString(text)!;
        }
        catch (Exception ex) when (ex is FormatException or NotSupportedException or ArgumentException)
        {
            throw new ValidateException("value", $"Giá trị không hợp lệ cho {RuleFormat.LowerFirst(u.Label)}.");
        }

        var taken = await DL.IsValueTakenAsync(u.Property.Name, typed, excludeId, ct);
        return new UniqueCheckResult { IsTaken = taken, Message = taken ? u.FormatMessage(text) : null };
    }

    protected static void AddError(IDictionary<string, List<string>> errors, string field, string message)
    {
        var key = JsonNamingPolicy.CamelCase.ConvertName(field);
        if (!errors.TryGetValue(key, out var list))
        {
            list = new List<string>();
            errors[key] = list;
        }
        list.Add(message);
    }

    /// <summary>
    /// Bọc action trong transaction. Nếu đã có transaction bên ngoài (BL khác gọi vào) thì tham gia luôn,
    /// không mở transaction lồng.
    /// </summary>
    protected async Task<TResult> RunInTransactionAsync<TResult>(Func<Task<TResult>> action, CancellationToken ct)
    {
        if (UnitOfWork.HasTransaction) return await action();

        await UnitOfWork.BeginAsync(ct);
        try
        {
            var result = await action();
            await UnitOfWork.CommitAsync(ct);
            return result;
        }
        catch
        {
            await UnitOfWork.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    /// <summary>UPDATE/DELETE không ảnh hưởng dòng nào: hoặc không tồn tại, hoặc RowVersion đã cũ.</summary>
    protected async Task<Exception> NotFoundOrConcurrencyAsync(Guid id, CancellationToken ct)
        => await DL.ExistsAsync(id, ct)
            ? new ConcurrencyException(EntityName, id)
            : new NotFoundException(EntityName, id);
}
