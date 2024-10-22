namespace CSharpManager.Extensions;

public static unsafe class UnsafeExtension
{
    public static string FormatHex(this nuint value) => sizeof(nuint) == 4 ? $"0x{(uint)value:X8}" : $"0x{(ulong)value:X16}";

    public static bool IsValidPage(this PageInfo pageInfo) =>
        pageInfo.Protection != 0 && (pageInfo.Protection & MemoryProtection.NoAccess) == 0 && (ulong)pageInfo.Size <= int.MaxValue;
}