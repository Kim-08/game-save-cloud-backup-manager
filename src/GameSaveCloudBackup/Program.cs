using GameSaveCloudBackup.Services;
using GameSaveCloudBackup.UI;

namespace GameSaveCloudBackup;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        ApplicationConfiguration.Initialize();

        var loggingService = new LoggingService();
        Application.ThreadException += (_, eventArgs) =>
        {
            loggingService.Error("Unhandled UI exception", eventArgs.Exception);
            MessageBox.Show($"Something failed unexpectedly. Details were written to the log.\n\n{eventArgs.Exception.Message}", "Game Save Cloud Backup Manager", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        };
        AppDomain.CurrentDomain.UnhandledException += (_, eventArgs) =>
        {
            if (eventArgs.ExceptionObject is Exception ex)
            {
                loggingService.Error("Unhandled app exception", ex);
            }
        };

        loggingService.Info("App startup");

        var configService = new ConfigService(loggingService);
        var rcloneService = new RcloneService(loggingService);
        var backupService = new BackupService(rcloneService, loggingService);
        using var gameMonitorService = new GameMonitorService(backupService, loggingService);
        var appConfig = configService.Load();

        Application.Run(new MainForm(configService, loggingService, rcloneService, backupService, gameMonitorService, appConfig));
    }
}
