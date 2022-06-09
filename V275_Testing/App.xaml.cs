using ControlzEx.Theming;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;

namespace V275_Testing
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static Databases.SimpleDatabase Settings { get; private set; }

        //#if DEBUG
        public static string WorkingDir { get; set; } = System.IO.Directory.GetCurrentDirectory();
        //#else        
        //        public static string WorkingDir { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\TDD\\V275_Testing";
        //#endif

        public static string Version { get; set; }

        public static string UserDataDirectory => $"{WorkingDir}\\UserData";
        public static string DatabaseExtension => ".sqlite";

        public static string SettingsDatabaseName => $"ApplicationSettings{DatabaseExtension}";

        public static string StandardsRoot => $"{WorkingDir}\\Assets\\Standards";
        public static string StandardsDatabaseName => $"Standards{DatabaseExtension}";

        public static string RunsRoot => $"{UserDataDirectory}\\Runs";
        public static string RunLedgerDatabaseName => $"RunLedger{DatabaseExtension}";
        public static string RunDatabaseName(long timeDate) => $"Run_{timeDate}{DatabaseExtension}";

        public App()
        {
            SQLitePCL.Batteries.Init();

            var config = new NLog.Config.LoggingConfiguration();

            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = "UserData/log.txt",
                ArchiveFileName = "UserData/log.${shortdate}.txt",
                ArchiveAboveSize = 5242880,
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Rolling,
                MaxArchiveFiles = 3
            };
            config.AddRuleForAllLevels(logfile);
            NLog.LogManager.Configuration = config;

            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            NLog.LogManager.GetCurrentClassLogger().Info($"Starting: {Version}");

            if (!Directory.Exists(UserDataDirectory))
            {
                _ = Directory.CreateDirectory(UserDataDirectory);
            }
            if (!Directory.Exists(RunsRoot))
            {
                _ = Directory.CreateDirectory(RunsRoot);
            }

            Settings = new Databases.SimpleDatabase().Open(Path.Combine(UserDataDirectory, SettingsDatabaseName));

            if (Settings == null)
            {
                return;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Set the application theme to Dark.Green
            _ = ThemeManager.Current.ChangeTheme(this, Settings.GetValue("App.Theme", "Dark.Steel"));

            ThemeManager.Current.ThemeChanged += Current_ThemeChanged;
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        }
        private void Current_ThemeChanged(object sender, ThemeChangedEventArgs e) => App.Settings.SetValue("App.Theme", e.NewTheme.Name);

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Settings.Dispose();
        }
    }
}
