using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace GameSaveCloudBackup.Services;

public sealed class RcloneService
{
    private static readonly Regex SensitiveArgumentPattern = new(
        "(?i)(--(?:password|pass|token|secret|client-secret|drive-client-secret|s3-secret-access-key)(?:=|\\s+))([^\\s\"]+|\"[^\"]*\")",
        RegexOptions.Compiled);

    private readonly LoggingService _loggingService;

    public RcloneService(LoggingService loggingService)
    {
        _loggingService = loggingService;
    }

    public async Task<bool> CheckRcloneInstalled(CancellationToken cancellationToken = default)
    {
        var result = await RunRcloneCommandAsync(["version"], cancellationToken);
        return result.Succeeded;
    }

    public async Task<string?> GetRcloneVersion(CancellationToken cancellationToken = default)
    {
        var result = await RunRcloneCommandAsync(["version"], cancellationToken);
        if (!result.Succeeded)
        {
            return null;
        }

        return result.StandardOutput
            .Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault()
            ?.Trim();
    }

    public async Task<IReadOnlyList<string>> ListRemotes(CancellationToken cancellationToken = default)
    {
        var result = await RunRcloneCommandAsync(["listremotes"], cancellationToken);
        if (!result.Succeeded)
        {
            return [];
        }

        return result.StandardOutput
            .Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(remote => remote.Trim())
            .Where(remote => !string.IsNullOrWhiteSpace(remote))
            .Select(remote => remote.TrimEnd(':'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(remote => remote, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<bool> TestRemote(string remoteName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remoteName))
        {
            return false;
        }

        var normalizedRemote = NormalizeRemoteName(remoteName);
        var result = await RunRcloneCommandAsync(["lsd", normalizedRemote + ":"], cancellationToken);
        return result.Succeeded;
    }

    public async Task<string?> ReadRemoteTextFile(string remotePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return null;
        }

        var result = await RunRcloneCommandAsync(["cat", remotePath], cancellationToken);
        return result.Succeeded ? result.StandardOutput : null;
    }

    public async Task<IReadOnlyList<string>> ListRemoteDirectories(string remotePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return [];
        }

        var result = await RunRcloneCommandAsync(["lsf", remotePath, "--dirs-only"], cancellationToken);
        if (!result.Succeeded)
        {
            return [];
        }

        return result.StandardOutput
            .Split([Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim().TrimEnd('/'))
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderByDescending(line => line, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public async Task<RcloneCommandResult> DeleteRemoteDirectory(string remotePath, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(remotePath))
        {
            return RcloneCommandResult.Failed(string.Empty, "Remote path is required.");
        }

        return await RunRcloneCommandAsync(["purge", remotePath], cancellationToken);
    }

    public async Task<RcloneCommandResult> RunRcloneCommandAsync(IReadOnlyList<string> arguments, CancellationToken cancellationToken = default)
    {
        if (arguments.Count == 0 || arguments.Any(string.IsNullOrWhiteSpace))
        {
            return RcloneCommandResult.Failed(string.Empty, "rclone arguments are required.");
        }

        var safeArguments = FormatArgumentsForLog(arguments);
        _loggingService.Info($"Starting rclone command: rclone {safeArguments}");

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "rclone",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            foreach (var argument in arguments)
            {
                process.StartInfo.ArgumentList.Add(argument);
            }

            process.OutputDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is not null)
                {
                    stdout.AppendLine(eventArgs.Data);
                }
            };
            process.ErrorDataReceived += (_, eventArgs) =>
            {
                if (eventArgs.Data is not null)
                {
                    stderr.AppendLine(eventArgs.Data);
                }
            };

            process.Start();
            using var cancellationRegistration = cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill(entireProcessTree: true);
                    }
                }
                catch
                {
                    // Best effort. The command result below still reports cancellation.
                }
            });

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);
            stopwatch.Stop();

            var result = new RcloneCommandResult(
                safeArguments,
                process.ExitCode,
                stdout.ToString(),
                stderr.ToString(),
                stopwatch.Elapsed);

            LogResult(result);
            return result;
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            var result = new RcloneCommandResult(
                safeArguments,
                -2,
                stdout.ToString(),
                "rclone command was canceled.",
                stopwatch.Elapsed);
            _loggingService.Info($"rclone command canceled: rclone {safeArguments}");
            return result;
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 2)
        {
            stopwatch.Stop();
            var result = new RcloneCommandResult(
                safeArguments,
                -1,
                string.Empty,
                "rclone was not found. Install rclone and make sure it is available in PATH.",
                stopwatch.Elapsed);
            _loggingService.Error("rclone command failed: rclone executable not found", ex);
            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            var result = new RcloneCommandResult(
                safeArguments,
                -1,
                stdout.ToString(),
                ex.Message,
                stopwatch.Elapsed);
            _loggingService.Error($"rclone command failed: rclone {safeArguments}", ex);
            return result;
        }
    }

    public string BuildRemotePath(string remoteName, string cloudPath, string? optionalSubPath = null)
    {
        var normalizedRemote = NormalizeRemoteName(remoteName);
        var normalizedCloudPath = NormalizePathPart(cloudPath);
        var normalizedSubPath = NormalizePathPart(optionalSubPath);

        var pathParts = new[] { normalizedCloudPath, normalizedSubPath }
            .Where(part => !string.IsNullOrWhiteSpace(part));
        return $"{normalizedRemote}:{string.Join('/', pathParts)}";
    }

    private void LogResult(RcloneCommandResult result)
    {
        var stderrPreview = RedactSensitiveText(result.StandardError.Trim());
        if (stderrPreview.Length > 500)
        {
            stderrPreview = stderrPreview[..500] + "...";
        }

        var message = $"rclone command completed: rclone {result.Arguments}; exit={result.ExitCode}; durationMs={result.Duration.TotalMilliseconds:0}; stdoutBytes={result.StandardOutput.Length}; stderrBytes={result.StandardError.Length}";
        if (!string.IsNullOrWhiteSpace(stderrPreview))
        {
            message += $"; stderr={stderrPreview}";
        }

        if (result.Succeeded)
        {
            _loggingService.Info(message);
        }
        else
        {
            _loggingService.Error(message);
        }
    }

    private static string NormalizeRemoteName(string remoteName)
    {
        return remoteName.Trim().TrimEnd(':');
    }

    private static string NormalizePathPart(string? path)
    {
        return string.IsNullOrWhiteSpace(path)
            ? string.Empty
            : path.Trim().Trim('/', '\\').Replace('\\', '/');
    }

    private static string FormatArgumentsForLog(IEnumerable<string> arguments)
    {
        return RedactSensitiveText(string.Join(' ', arguments.Select(QuoteForDisplay)));
    }

    private static string QuoteForDisplay(string value)
    {
        if (value.Length == 0)
        {
            return "\"\"";
        }

        var needsQuotes = value.Any(char.IsWhiteSpace) || value.Contains('"');
        if (!needsQuotes)
        {
            return value;
        }

        return $"\"{value.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"";
    }

    private static string RedactSensitiveText(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        return SensitiveArgumentPattern.Replace(text, "$1[REDACTED]");
    }
}

public sealed record RcloneCommandResult(
    string Arguments,
    int ExitCode,
    string StandardOutput,
    string StandardError,
    TimeSpan Duration)
{
    public bool Succeeded => ExitCode == 0;

    public static RcloneCommandResult Failed(string arguments, string error) => new(arguments, -1, string.Empty, error, TimeSpan.Zero);
}
