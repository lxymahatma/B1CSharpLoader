using CSharpManager.Extensions;

namespace CSharpManager.Ini;

public sealed class IniReader : IniBase
{
    private readonly string _filePath;
    private readonly List<IniSection> _iniSections = [];
    private readonly Dictionary<string, Dictionary<string, string>> _sectionsCache = [];
    private bool _isFullyRead;
    private bool _isParsed;
    private long _position;

    public IniReader(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"{filePath} doesn't exist");
        }

        _filePath = filePath;
    }

    public List<IniSection> ParseFile()
    {
        if (_isParsed)
        {
            return _iniSections;
        }

        if (_isFullyRead)
        {
            return ConvertFromCache();
        }

        var currentSection = new IniSection(DefaultSection);
        _iniSections.Add(currentSection);

        foreach (var line in File.ReadLines(_filePath).Where(IsValidLine).Select(x => x.Trim()))
        {
            if (TryParseSectionName(line, out var sectionName))
            {
                currentSection = new IniSection(sectionName);
                _iniSections.Add(currentSection);
            }
            else if (TryParseKeyValuePair(line, out var key, out var value))
            {
                currentSection.KeyValuePairs[key] = value;
            }
        }

        _isParsed = true;
        return _iniSections;
    }

    public string GetValue(string sectionName, string key, string defaultValue = "")
    {
        if (_isParsed)
        {
            ConvertFromSections();
        }

        if (TryGetValueFromCache(sectionName, key, out var cachedValue))
        {
            return cachedValue;
        }

        return _isFullyRead ? defaultValue : ReadSectionUntilKey(sectionName, key, defaultValue);
    }

    private string ReadSectionUntilKey(string sectionName, string key, string defaultValue)
    {
        using var fs = new FileStream(_filePath, FileMode.Open, FileAccess.Read);
        using var sr = new StreamReader(fs);

        fs.Seek(_position, SeekOrigin.Begin);
        var currentSectionName = DefaultSection;
        _sectionsCache[currentSectionName] = new Dictionary<string, string>();

        while (sr.ReadLine() is { } line)
        {
            if (!IsValidLine(line))
            {
                continue;
            }

            line = line.Trim();

            if (TryParseSectionName(line, out var parsedSectionName))
            {
                currentSectionName = parsedSectionName;
                _sectionsCache[currentSectionName] = [];
                continue;
            }

            if (!TryParseKeyValuePair(line, out var parsedKey, out var parsedValue))
            {
                continue;
            }

            _sectionsCache[currentSectionName][parsedKey] = parsedValue;

            if (currentSectionName != sectionName || parsedKey != key)
            {
                continue;
            }

            _position = fs.Position;
            _isFullyRead = fs.CanRead;

            return parsedValue;
        }

        _isFullyRead = true;
        return defaultValue;
    }

    private bool IsValidLine(string line) => !string.IsNullOrWhiteSpace(line) && !CommentChars.Contains(line[0]);

    private static bool TryParseSectionName(string line, out string sectionName)
    {
        sectionName = string.Empty;
        if (line.Length <= 2 || line[0] != '[' || line[line.Length - 1] != ']')
        {
            return false;
        }

        sectionName = line.Substring(1, line.Length - 2).Trim();
        return !sectionName.IsNullOrEmpty();
    }

    private static bool TryParseKeyValuePair(string line, out string key, out string value)
    {
        var keyValue = line.Split('=');
        if (keyValue.Length == 2)
        {
            key = keyValue[0].Trim();
            value = keyValue[1].Trim();
            return true;
        }

        key = value = string.Empty;
        return false;
    }

    private bool TryGetValueFromCache(string sectionName, string key, out string value)
    {
        if (_sectionsCache.TryGetValue(sectionName, out var section) && section.TryGetValue(key, out value))
        {
            return true;
        }

        value = string.Empty;
        return false;
    }

    private List<IniSection> ConvertFromCache()
    {
        foreach (var (sectionName, keyValue) in _sectionsCache)
        {
            var section = new IniSection(sectionName);
            foreach (var (key, value) in keyValue)
            {
                section.KeyValuePairs[key] = value;
            }

            _iniSections.Add(section);
        }

        _isParsed = true;
        return _iniSections;
    }

    private void ConvertFromSections()
    {
        foreach (var section in _iniSections)
        {
            if (!_sectionsCache.ContainsKey(section.SectionName))
            {
                _sectionsCache[section.SectionName] = [];
            }

            foreach (var (key, value) in section.KeyValuePairs)
            {
                if (_sectionsCache[section.SectionName].ContainsKey(key))
                {
                    continue;
                }

                _sectionsCache[section.SectionName][key] = value;
            }
        }

        _isFullyRead = true;
    }
}