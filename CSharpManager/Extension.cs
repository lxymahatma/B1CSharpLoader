using System.Text.RegularExpressions;

namespace CSharpManager;

public static unsafe class Extension
{
    public static string FormatHex(this nuint value) => sizeof(nuint) == 4 ? $"0x{(uint)value:X8}" : $"0x{(ulong)value:X16}";

    public static bool IsValidPage(this PageInfo pageInfo) =>
        pageInfo.Protection != 0 && (pageInfo.Protection & MemoryProtection.NoAccess) == 0 && (ulong)pageInfo.Size <= int.MaxValue;

    public static string RemoveInvalidChars(this string str)
    {
        var invalidFileNameChars = new string(Path.GetInvalidFileNameChars());
        var invalidCharRegex = new Regex($"[{Regex.Escape(invalidFileNameChars)}]");
        return invalidCharRegex.Replace(str, "_");
    }
}