namespace CSharpManager.Extensions;

public static class DeconstructExtensions
{
    public static void Deconstruct(this KeyValuePair<string, string> keyValuePair, out string key, out string value)
    {
        key = keyValuePair.Key;
        value = keyValuePair.Value;
    }

    public static void Deconstruct(this KeyValuePair<string, Dictionary<string, string>> keyValuePair, out string key, out Dictionary<string, string> value)
    {
        key = keyValuePair.Key;
        value = keyValuePair.Value;
    }
}