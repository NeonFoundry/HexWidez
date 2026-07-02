using System.IO;
using System.Windows;
using Serilog;

namespace HexWide
{
    public partial class App : Application
    {
        public App()
        {
            var settings = AppSettingsLoader.Load();
            string logPath = Path.Combine(AppContext.BaseDirectory, settings.LogFilePath);
            string logDirectory = Path.GetDirectoryName(logPath) ?? AppContext.BaseDirectory;
            Directory.CreateDirectory(logDirectory);

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.File(
                    logPath,
                    rollingInterval: RollingInterval.Day,
                    retainedFileCountLimit: settings.MaxLogFileCount,
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            Log.Information("HexWide starting up.");
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.CloseAndFlush();
            base.OnExit(e);
        }
    }
}
