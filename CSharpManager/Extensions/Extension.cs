using System.Text;
using System.Text.RegularExpressions;

namespace CSharpManager.Extensions;

public static class Extension
{
    public static string RemoveInvalidChars(this string str)
    {
        var invalidFileNameChars = new string(Path.GetInvalidFileNameChars());
        var invalidCharRegex = new Regex($"[{Regex.Escape(invalidFileNameChars)}]");
        return invalidCharRegex.Replace(str, "_");
    }

    public static string ToHexString(this byte[]? bytes)
    {
        if (bytes is null)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        foreach (var b in bytes)
        {
            sb.AppendFormat("{0:x2}", b);
        }

        return sb.ToString();
    }

    public async static void Await(this Task task, Action? onCompleted = null, Action<Exception>? onError = null)
    {
        try
        {
            await task;
            onCompleted?.Invoke();
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
        }
    }
}