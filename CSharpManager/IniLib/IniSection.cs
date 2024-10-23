using System.Text;
using CSharpManager.Extensions;

namespace CSharpManager.Ini;

public sealed class IniSection(string sectionName)
{
    public readonly Dictionary<string, string> KeyValuePairs = [];
    public string SectionName { get; } = sectionName;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Section: {SectionName}");

        foreach (var (key, value) in KeyValuePairs)
        {
            sb.AppendLine($"Key: {key}    Value: {value}");
        }

        return sb.ToString();
    }
}