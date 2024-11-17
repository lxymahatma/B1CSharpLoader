namespace CSharpModBase;

public static class Log
{
    private static readonly StreamWriter LogFile = File.CreateText(Path.Combine(Common.LoaderDir, "CSharpLog.log"));
    private static readonly LogLevel _logLevel = LogLevel.Info;
    private static string DateTimeString => DateTime.Now.ToString("MM-dd HH:mm:ss"); // .fff

    public static void Info(string message)
    {
        var text = $"{DateTimeString} [I] {message}";
        WriteLine(text, LogLevel.Info, ConsoleColor.White);
    }

    public static void Debug(string message)
    {
        var text = $"{DateTimeString} [D] {message}";
        WriteLine(text, LogLevel.Debug, ConsoleColor.Gray);
    }

    public static void Warn(string message)
    {
        var text = $"{DateTimeString} [W] {message}";
        WriteLine(text, LogLevel.Warn, ConsoleColor.Yellow);
    }

    public static void Error(string message)
    {
        var text = $"{DateTimeString} [E] {message}";
        WriteLine(text, LogLevel.Error, ConsoleColor.Red);
    }

    public static void Error(Exception e)
    {
        Error(e.Message);
        Error(e.StackTrace);
    }

    private static void WriteLine(string message, LogLevel level, ConsoleColor color)
    {
        if (level < _logLevel)
        {
            return;
        }

        using (new ChangeConsoleColor(color))
        {
            Console.WriteLine(message);
        }

        LogFile.WriteLine(message);
    }
}

public readonly ref struct ChangeConsoleColor : IDisposable
{
    private readonly ConsoleColor _currentForeground;

    public ChangeConsoleColor(ConsoleColor color)
    {
        _currentForeground = Console.ForegroundColor;
        Console.ForegroundColor = color;
    }

    public void Dispose()
    {
        Console.ForegroundColor = _currentForeground;
    }
}