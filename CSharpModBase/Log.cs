namespace CSharpModBase;

public static class Log
{
    private static readonly StreamWriter LogFile = File.CreateText(Path.Combine(Common.LoaderDir, "CSharpLog.log"));
    internal static LogLevel LogLevel { get; set; } = LogLevel.Info;
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
        if (level < LogLevel)
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

public readonly ref struct ChangeConsoleColor
{
    public ChangeConsoleColor(ConsoleColor color) => Console.ForegroundColor = color;

    public void Dispose() => Console.ResetColor();
}