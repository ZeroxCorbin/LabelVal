using ControlzEx.Theming;
using LabelVal.Databases;
using System;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using V275_REST_lib.Models;

namespace LabelVal
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
        public static string WorkingDir { get; set; } = $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\LabelVal_Data";
#endif

        public static string Version { get; set; }

        public static string UserDataDirectory => $"{WorkingDir}\\UserData";
        public static string DatabaseExtension => ".sqlite";

        public static string SettingsDatabaseName => $"ApplicationSettings{DatabaseExtension}";

        public static string AssetsStandardsDatabasesRoot => $"{System.IO.Directory.GetCurrentDirectory()}\\Assets\\StandardsDatabases";
        public static string AssetsStandardsRoot => $"{System.IO.Directory.GetCurrentDirectory()}\\Assets\\Standards";

        public static string StandardsDatabaseRoot => $"{UserDataDirectory}\\StandardsDatabases";
        public static string StandardsRoot => $"{UserDataDirectory}\\Standards";

        public static string StandardsDatabaseDefaultName => $"StandardsDatabase";

        public static string RunsRoot => $"{UserDataDirectory}\\Runs";
        public static string RunLedgerDatabaseName => $"RunLedger{DatabaseExtension}";
        public static string RunDatabaseName(long timeDate) => $"Run_{timeDate}{DatabaseExtension}";

        public App()
        {
            //   ExtractRunDetails();

            SetupExceptionHandling();

            Version = Assembly.GetExecutingAssembly().GetName().Version.ToString();

            if (!Directory.Exists(UserDataDirectory))
                _ = Directory.CreateDirectory(UserDataDirectory);
            if (!Directory.Exists(StandardsDatabaseRoot))
                _ = Directory.CreateDirectory(StandardsDatabaseRoot);
            if (!Directory.Exists(StandardsRoot))
                _ = Directory.CreateDirectory(StandardsRoot);

            if (!Directory.Exists(RunsRoot))
                _ = Directory.CreateDirectory(RunsRoot);

            var config = new NLog.Config.LoggingConfiguration();
            // Targets where to log to: File and Console
            var logfile = new NLog.Targets.FileTarget("logfile")
            {
                FileName = Path.Combine(UserDataDirectory, "log.txt"),
                ArchiveFileName = Path.Combine(UserDataDirectory, "log.${shortdate}.txt"),
                ArchiveAboveSize = 5242880,
                ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
                ArchiveNumbering = NLog.Targets.ArchiveNumberingMode.Rolling,
                MaxArchiveFiles = 3
            };
            config.AddRuleForAllLevels(logfile);
            NLog.LogManager.Configuration = config;

            NLog.LogManager.GetCurrentClassLogger().Info($"Starting: {Version}");

            try
            {
                SQLitePCL.Batteries.Init();
            }
            catch (Exception ex)
            {
                NLog.LogManager.GetCurrentClassLogger().Error(ex);
                Shutdown();
                return;
            }

            Settings = new Databases.SimpleDatabase().Open(Path.Combine(UserDataDirectory, SettingsDatabaseName));

            if (Settings == null)
            {
                NLog.LogManager.GetCurrentClassLogger().Error("The ApplicationSettings database is null. Shutdown!");
                Shutdown();
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            //RedrawFiducial(@"D:\OneDrive - OMRON\Omron\OCR\Applications\LabelVal\LabelVal\Assets\Standards\VALIDATION\300\FINAL 300dpi TEST ROLL 4x4 50labels_49a.png");
            ChangeColorBlindTheme(App.Settings.GetValue("App.IsColorBlind", false));

            // Set the application theme to Dark.Green
            _ = ThemeManager.Current.ChangeTheme(this, Settings.GetValue("App.Theme", "Dark.Steel"));

            ThemeManager.Current.ThemeChanged += Current_ThemeChanged;
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        }
        private void Current_ThemeChanged(object sender, ThemeChangedEventArgs e) => App.Settings.SetValue("App.Theme", e.NewTheme.Name);

        public static void ChangeColorBlindTheme(bool isColorBlind)
        {
            App.Settings.SetValue("App.IsColorBlind", isColorBlind);

            Application.Current.Resources["CB_Green"] = isColorBlind
                ? Application.Current.Resources["ColorBlindBrush1"]
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
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
            var message = $"Unhandled exception ({source})";
            try
            {
                var assemblyName = System.Reflection.Assembly.GetExecutingAssembly().GetName();
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

            _ = MessageBox.Show($"{message}\r\n{exception.Message}", "Unhandled Exception!", MessageBoxButton.OK);
            Shutdown();

        }

        private void FixFiducial()
        {
            foreach (var dir in Directory.EnumerateDirectories(AssetsStandardsRoot))
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
            using (var fs = new FileStream(path, FileMode.Open))
            {
                var photo = (Bitmap)Bitmap.FromStream(fs);
                fs.Close();
                var newmap = new Bitmap(photo.Width, photo.Height);
                newmap.SetResolution(photo.HorizontalResolution, photo.VerticalResolution);
                //if (photo.Height != 2400)
                //    File.AppendAllText($"{UserDataDirectory}\\Small Images List", Path.GetFileName(path));

                //600 DPI
                //if ((photo.Height > 2400 && photo.Height != 4800) || photo.Height < 2000)
                //    return;

                //300 DPI
                if ((photo.Height > 1200) || photo.Height < 1000)
                    return;

                using (var graphics = Graphics.FromImage(newmap))
                {
                    graphics.DrawImage(photo, 0, 0, photo.Width, photo.Height);
                    //graphics.FillRectangle(Brushes.White, 0, 1900, 210, photo.Height - 1900);
                    //graphics.FillRectangle(Brushes.Black, 30, 1950, 90, 90);

                    //300 DPI
                    graphics.FillRectangle(Brushes.White, 0, 976, 150, photo.Height - 976);
                    graphics.FillRectangle(Brushes.Black, 15, 975, 45, 45);

                    newmap.Save(path, ImageFormat.Png);
                }
            }
        }

        private void FixRotation()
        {
            foreach (var dir in Directory.EnumerateDirectories(AssetsStandardsRoot))
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
            using (var fs = new FileStream(path, FileMode.Open))
            {
                var photo = Bitmap.FromStream(fs);
                fs.Close();

                photo.RotateFlip(RotateFlipType.Rotate180FlipNone);
                photo.Save(path, ImageFormat.Png);

            }
        }

        private class FinalReport
        {

            public string LogName { get; set; }
            public string TemplateName { get; set; }
            public string Operator { get; set; }

            public string StartTime { get; set; }
            public string EndTime { get; set; }

            public int Inspected { get; set; }
            public int GoodAccepted { get; set; }
            public int Failed { get; set; }
            public int FailedAccepted { get; set; }
            public int Removed { get; set; }
            public int Voided { get; set; }
        }
        private void ExtractRunDetails()
        {
            var db = new V275RunDatabase().Open(@"C:\Users\Jack\GitHub\LabelVal\LabelVal\bin\Debug\_p-000334 rev. 2_RunLog_Run10.db");
            var entries = db.SelectAllRunEntries().OrderBy(v => v.cycleID).ToList();

            var final = new FinalReport();

            var first = true;
            foreach (var entry in entries)
            {
                var report = Newtonsoft.Json.JsonConvert.DeserializeObject<Report>(entry.reportData);

                if (first)
                {
                    final.LogName = "p-000334 rev. 2_RunLog_Run10";
                    final.TemplateName = "p-000334 rev. 2";
                    final.Operator = "epalacio";
                    final.StartTime = entry.timeStamp;
                    final.EndTime = entries.Last().timeStamp;
                    final.Inspected = entries.Count;
                    first = false;
                }

                if (report.inspectLabel.result == "pass")
                {
                    final.GoodAccepted++;
                }
                else
                {
                    final.Failed++;
                    if (report.inspectLabel.userAction.action == "accepted")
                        final.FailedAccepted++;
                    else if (report.inspectLabel.userAction.action == "removed")
                        final.Removed++;
                    else if (report.inspectLabel.userAction.action == "voided")
                        final.Voided++;
                }
            }


            File.WriteAllText("result.json", Newtonsoft.Json.JsonConvert.SerializeObject(final));
        }

        // create an image of the desired size

        // save image to file or stream

        //}
    }
}
