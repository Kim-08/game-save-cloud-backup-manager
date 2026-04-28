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
        loggingService.Info("App startup");

        var configService = new ConfigService(loggingService);
        var appConfig = configService.Load();

        Application.Run(new MainForm(configService, loggingService, appConfig));
    }
}
