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


            if (!Directory.Exists(UserDataDirectory))
            {
                _ = Directory.CreateDirectory(UserDataDirectory);
            }
            if (!Directory.Exists(RunsRoot))
            {
                _ = Directory.CreateDirectory(RunsRoot);
            }

            //FixFiducial();
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

            // Set the application theme to Dark.Green
            _ = ThemeManager.Current.ChangeTheme(this, Settings.GetValue("App.Theme", "Dark.Steel"));

            ThemeManager.Current.ThemeChanged += Current_ThemeChanged;
            ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
        }
        private void Current_ThemeChanged(object sender, ThemeChangedEventArgs e) => App.Settings.SetValue("App.Theme", e.NewTheme.Name);

        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            Settings?.Dispose();
        }

        private void FixFiducial()
        {
            foreach (var dir in Directory.EnumerateDirectories(StandardsRoot))
            {
                if (Directory.Exists($"{dir}\\300"))
                    foreach (var imgFile in Directory.EnumerateFiles($"{dir}\\300"))
                    {
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

                    //graphics.FillRectangle(Brushes.White, 0, 1952, 180, photo.Height - 1952);
                    //graphics.FillRectangle(Brushes.Black, 39, 2000, 106, 158);

                    //300 DPI
                    graphics.FillRectangle(Brushes.White, 0, 976, 150, photo.Height - 976);
                    graphics.FillRectangle(Brushes.Black, 20, 1000, 53, 79);

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
