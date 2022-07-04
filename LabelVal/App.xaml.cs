using ControlzEx.Theming;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Principal;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal
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
        //        public static string WorkingDir { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\TDD\\LabelVal";
        //#endif

        public static string Version { get; set; }

        public static string UserDataDirectory => $"{WorkingDir}\\UserData";
        public static string DatabaseExtension => ".sqlite";

        public static string SettingsDatabaseName => $"ApplicationSettings{DatabaseExtension}";

        public static string StandardsRoot => $"{WorkingDir}\\Assets\\Standards";
        public static string StandardsDatabaseRoot => $"{UserDataDirectory}\\StandardsDatabases";
        public static string StandardsDatabaseDefaultName => $"StandardsDatabase";

        public static string RunsRoot => $"{UserDataDirectory}\\Runs";
        public static string RunLedgerDatabaseName => $"RunLedger{DatabaseExtension}";
        public static string RunDatabaseName(long timeDate) => $"Run_{timeDate}{DatabaseExtension}";

        public App()
        {
            SetupExceptionHandling();

            if (!Directory.Exists(UserDataDirectory))
            {
                _ = Directory.CreateDirectory(UserDataDirectory);
            }
            if (!Directory.Exists(RunsRoot))
            {
                _ = Directory.CreateDirectory(RunsRoot);
            }
            if (!Directory.Exists(StandardsDatabaseRoot))
            {
                _ = Directory.CreateDirectory(StandardsDatabaseRoot);
            }
            // FixFiducial();
            //FixRotation();

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

            try
            {
                SQLitePCL.Batteries.Init();
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex);
                this.Shutdown();
                return;
            }

            Settings = new Databases.SimpleDatabase().Open(Path.Combine(UserDataDirectory, SettingsDatabaseName));

            if (Settings == null)
            {
                this.Shutdown();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            ChangeColorBlindTheme(App.Settings.GetValue("App.IsColorBlind", false));

            // Set the application theme to Dark.Green
            _ = ThemeManager.Current.ChangeTheme(this, Settings.GetValue("App.Theme", "Dark.Steel"));

            ThemeManager.Current.ThemeChanged += Current_ThemeChanged;
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        }
        private void Current_ThemeChanged(object sender, ThemeChangedEventArgs e)
        {
            App.Settings.SetValue("App.Theme", e.NewTheme.Name);
        }

        public static void ChangeColorBlindTheme(bool isColorBlind)
        {
            App.Settings.SetValue("App.IsColorBlind", isColorBlind);

            if (isColorBlind)
                Application.Current.Resources["CB_Green"] = Application.Current.Resources["ColorBlindBrush1"];
            else
                Application.Current.Resources["CB_Green"] = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Settings?.Dispose();
        }

        private void SetupExceptionHandling()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
                LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException");

            DispatcherUnhandledException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException");
                e.Handled = true;
            };

            TaskScheduler.UnobservedTaskException += (s, e) =>
            {
                LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException");
                e.SetObserved();
            };
        }

        private void LogUnhandledException(Exception exception, string source)
        {
            string message = $"Unhandled exception ({source})";
            try
            {
                System.Reflection.AssemblyName assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
                message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex, "Exception in LogUnhandledException");
            }
            finally
            {
                NLog.LogManager.GetCurrentClassLogger().Error(exception, message);
            }
        }


        private void FixFiducial()
        {
            foreach (var dir in Directory.EnumerateDirectories(StandardsRoot))
            {
                if (Directory.Exists($"{dir}\\300"))
                    foreach (var imgFile in Directory.EnumerateFiles($"{dir}\\300"))
                    {
                        if (Path.GetExtension(imgFile) == ".png")
                            RedrawFiducial(imgFile);
                    }
            }
        }

        private void RedrawFiducial(string path)
        {
            // load your photo
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                Image photo = Bitmap.FromStream(fs);
                fs.Close();

                //if (photo.Height != 2400)
                //    File.AppendAllText($"{UserDataDirectory}\\Small Images List", Path.GetFileName(path));

                //600 DPI
                //if ((photo.Height > 2400 && photo.Height != 4800) || photo.Height < 2000)
                //    return;

                //300 DPI
                if ((photo.Height > 1200) || photo.Height < 1000)
                    return;

                using (var graphics = Graphics.FromImage(photo))
                {

                    //graphics.FillRectangle(Brushes.White, 0, 1900, 210, photo.Height - 1900);
                    //graphics.FillRectangle(Brushes.Black, 30, 1950, 90, 90);

                    //300 DPI
                    graphics.FillRectangle(Brushes.White, 0, 976, 150, photo.Height - 976);
                    graphics.FillRectangle(Brushes.Black, 15, 975, 45, 45);

                    photo.Save(path, ImageFormat.Png);
                }
            }

        }

        private void FixRotation()
        {
            foreach (var dir in Directory.EnumerateDirectories(StandardsRoot))
            {
                foreach (var imgFile in Directory.EnumerateFiles($"{dir}\\600"))
                {
                    if (imgFile.Contains("PRINT QUALITY"))
                        RotateImage(imgFile);
                }
            }
        }

        private void RotateImage(string path)
        {
            // load your photo
            using (FileStream fs = new FileStream(path, FileMode.Open))
            {
                Image photo = Bitmap.FromStream(fs);
                fs.Close();

                photo.RotateFlip(RotateFlipType.Rotate180FlipNone);
                photo.Save(path, ImageFormat.Png);

            }

        }
        // create an image of the desired size


        // save image to file or stream

        //}
    }
}
