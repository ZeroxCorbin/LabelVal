using ControlzEx.Theming;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
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

#if DEBUG
        public static string WorkingDir { get; set; } = System.IO.Directory.GetCurrentDirectory();
#else        
        public static string WorkingDir { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\TDD\\V275_Testing\\";
#endif

        public static string UserDataDirectory => $"{WorkingDir}\\UserData";
        public static string DatabaseSettingsFile => "ApplicationSettings";
        public static string DatabaseExtension => ".sqlite";

        public static string StandardsRoot =>  $"{WorkingDir}\\Assets\\Standards";
        public static string StandardsDatabaseName => $"Standards{DatabaseExtension}";

        public App()
        {

            if (!Directory.Exists(UserDataDirectory))
            {
                _ = Directory.CreateDirectory(UserDataDirectory);
            }

            Settings = new Databases.SimpleDatabase().Init(Path.Combine(UserDataDirectory, $"{DatabaseSettingsFile}{DatabaseExtension}"), false);

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
