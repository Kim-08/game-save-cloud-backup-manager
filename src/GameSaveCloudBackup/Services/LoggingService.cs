namespace GameSaveCloudBackup.Services;

public sealed class LoggingService
{
    private const int TailReadBytes = 1024 * 1024;
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

            maxLines = Math.Max(1, maxLines);
            using var stream = new FileStream(LogFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
            if (stream.Length > TailReadBytes)
            {
                stream.Seek(-TailReadBytes, SeekOrigin.End);
            }

            using var reader = new StreamReader(stream);
            if (stream.Position > 0)
            {
                _ = reader.ReadLine(); // discard the first partial line from the tail window
            }

            var lines = new Queue<string>(maxLines);
            while (reader.ReadLine() is { } line)
            {
                lines.Enqueue(line);
                while (lines.Count > maxLines)
                {
                    lines.Dequeue();
                }
            }

            return lines.ToList();
        }
        catch
        {
            // Logs are diagnostic, not load-bearing. If another process locks the file,
            // keep the UI alive and try again on the next refresh.
            return [];
        }
    }

    private void Write(string level, string message)
    {
        var line = $"{DateTimeOffset.Now:O} [{level}] {message}";
        try
        {
            lock (_lock)
            {
                Directory.CreateDirectory(LogDirectory);
                using var stream = new FileStream(LogFilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
                using var writer = new StreamWriter(stream);
                writer.WriteLine(line);
            }
        }
        catch
        {
            // Never let logging crash the app. That would be very funny in the wrong way.
        }
    }
}
