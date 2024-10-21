namespace CSharpManager;

public static unsafe class Extension
{
    public static string FormatHex(this nuint value) => sizeof(nuint) == 4 ? $"0x{(uint)value:X8}" : $"0x{(ulong)value:X16}";
}