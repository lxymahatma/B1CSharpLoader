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