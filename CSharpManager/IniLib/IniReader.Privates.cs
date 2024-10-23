namespace CSharpManager.IniLib;

public sealed partial class IniReader
{
    private bool TryGetValue(string sectionName, string keyName, out string value)
    {
        var storedValue = ReadValueBySectionAndKey(sectionName, keyName);
        if (storedValue is null)
        {
            value = string.Empty;
            return false;
        }

        value = storedValue;
        return true;
    }

    private string? ReadValueBySectionAndKey(string sectionName, string keyName)
    {
        using var sr = new StreamReader(_filePath);
        var lineNumber = 0;
        var foundTargetSection = false;

        while (sr.ReadLine() is { } line)
        {
            lineNumber++;

            if (IsSkipLine(sectionName, lineNumber) || !IsValidLine(line))
            {
                continue;
            }

            line = line.Trim();

            if (IsSectionHeader(line, out var newSection))
            {
                // Store the start position of the newly encountered section
                _sectionStartPositionDict[newSection] = lineNumber;

                // If the new section is found after the target section, we break the loop
                if (foundTargetSection)
                {
                    break;
                }

                foundTargetSection = newSection == sectionName;
                continue;
            }

            if (foundTargetSection && TryParseKeyValue(line, out var key, out var value) && key == keyName)
            {
                return value;
            }
        }

        return null;
    }

    private bool IsValidLine(string line) => !string.IsNullOrWhiteSpace(line) && !CommentChars.Contains(line[0]);

    private bool IsSkipLine(string sectionName, int lineNumber) =>
        _sectionStartPositionDict.TryGetValue(sectionName, out var startPos) && lineNumber < startPos;

    private static bool IsSectionHeader(string line, out string sectionName)
    {
        sectionName = string.Empty;

        if (line.Length <= 2 || line[0] != '[' || line[^1] != ']')
        {
            return false;
        }

        sectionName = line[1..^1];
        return true;
    }

    private static bool TryParseKeyValue(string line, out string key, out string value)
    {
        key = value = string.Empty;
        var keyValue = line.Split('=');
        if (keyValue.Length != 2)
        {
            return false;
        }

        key = keyValue[0].Trim();
        value = keyValue[1].Trim().Trim('"');
        return true;
    }
}