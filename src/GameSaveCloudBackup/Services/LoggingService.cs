namespace GameSaveCloudBackup.Services;

public sealed class LoggingService
{
    private readonly object _lock = new();

    public string LogDirectory { get; }
    public string LogFilePath { get; }

    public LoggingService()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        LogDirectory = Path.Combine(appData, "GameSaveCloudBackup", "Logs");
        LogFilePath = Path.Combine(LogDirectory, "app.log");
        Directory.CreateDirectory(LogDirectory);
    }

    public void Info(string message) => Write("INFO", message);

    public void Error(string message, Exception? exception = null)
    {
        var fullMessage = exception is null ? message : $"{message} | {exception}";
        Write("ERROR", fullMessage);
    }

    public IReadOnlyList<string> GetRecentLines(int maxLines = 100)
    {
        try
        {
            if (!File.Exists(LogFilePath))
            {
                return [];
            }

            return File.ReadLines(LogFilePath).TakeLast(maxLines).ToList();
        }
        catch
        {
            return [];
        }
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:O} [{level}] {message}";
        lock (_lock)
        {
            Directory.CreateDirectory(LogDirectory);
            File.AppendAllText(LogFilePath, line + Environment.NewLine);
        }
    }
}
