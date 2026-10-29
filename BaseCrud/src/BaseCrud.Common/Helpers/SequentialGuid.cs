using System.Security.Cryptography;

namespace BaseCrud.Common.Helpers;

/// <summary>
/// Sinh Guid "tuần tự" theo thời gian, tối ưu cho SQL Server.
/// Guid.NewGuid() ngẫu nhiên hoàn toàn → mỗi INSERT rơi vào vị trí bất kỳ của clustered index
/// → page split, phân mảnh, chậm dần khi bảng lớn.
/// SQL Server so sánh uniqueidentifier theo byte 10..15 trước, nên đặt timestamp (ms) vào đó.
/// </summary>
public static class SequentialGuid
{
    public static Guid NewGuid()
    {
        Span<byte> bytes = stackalloc byte[16];
        RandomNumberGenerator.Fill(bytes);

        long ms = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        bytes[10] = (byte)(ms >> 40);
        bytes[11] = (byte)(ms >> 32);
        bytes[12] = (byte)(ms >> 24);
        bytes[13] = (byte)(ms >> 16);
        bytes[14] = (byte)(ms >> 8);
        bytes[15] = (byte)ms;

        return new Guid(bytes);
    }
}
