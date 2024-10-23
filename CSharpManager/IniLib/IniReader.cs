namespace CSharpManager.IniLib;

public sealed partial class IniReader : IniBase
{
    private readonly string _filePath;
    private readonly Dictionary<string, int> _sectionStartPositionDict = [];

    public IniReader(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"{filePath} doesn't exist");
        }

        _filePath = filePath;
    }

    public int GetInt(string sectionName, string keyName, int defaultValue = 0) =>
        TryGetValue(sectionName, keyName, out var value)
            ? Convert.ToInt32(value)
            : defaultValue;

    public bool GetBool(string sectionName, string keyName, bool defaultValue = false) =>
        TryGetValue(sectionName, keyName, out var value)
            ? Convert.ToBoolean(value)
            : defaultValue;

    public string GetString(string sectionName, string keyName, string defaultValue = "") =>
        TryGetValue(sectionName, keyName, out var value)
            ? value
            : defaultValue;
}