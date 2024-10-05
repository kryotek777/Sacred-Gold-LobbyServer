namespace Sacred;

internal static class Log
{
    private static object writeLock = new();

    private static StreamWriter? logFile;

    /// <summary>
    /// The minimum logging level needed to actually write the message, anything lower than this value won't be written.
    /// </summary>
    public static LogSeverity LogLevel { get; set; } = LogSeverity.Trace;

    public static void Initialize(LogSeverity logLevel, string? logPath)
    {
        LogLevel = logLevel;

        if(logPath != null)
        {
            try
            {
                logFile?.Dispose();
                logFile = new StreamWriter(File.Open(logPath, FileMode.OpenOrCreate));
            }
            catch(Exception ex)
            {
                Error($"Error opening log file at path '{logPath}': {ex.Message}");
            }
        }
    }

    private static void Write(LogSeverity severity, string msg)
    {
        // Filter the unwanted messages
        if (severity >= LogLevel)
        {
            // Single calls to the Console class are thread safe.
            // However, we still need to lock to prevent colors from changing out of order
            lock (writeLock)
            {
                string line = $"[{DateTime.Now:u}] [{severity}] {msg}";

                Console.ForegroundColor = GetSeverityColor(severity);
                Console.WriteLine(line);
                Console.ResetColor();

                logFile?.WriteLine(line);
                logFile?.Flush();
            }
        }
    }

    private static ConsoleColor GetSeverityColor(in LogSeverity severity) => severity switch
    {
        LogSeverity.Trace => ConsoleColor.Gray,
        LogSeverity.Info => ConsoleColor.Cyan,
        LogSeverity.Warning => ConsoleColor.Yellow,
        LogSeverity.Error => ConsoleColor.Red,
        _ => ConsoleColor.White
    };

    public static void Trace(string msg) => Write(LogSeverity.Trace, msg);
    public static void Info(string msg) => Write(LogSeverity.Info, msg);
    public static void Warning(string msg) => Write(LogSeverity.Warning, msg);
    public static void Error(string msg) => Write(LogSeverity.Error, msg);
}
