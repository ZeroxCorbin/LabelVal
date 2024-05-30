using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using ControlzEx.Theming;
using LibSimpleDatabase;
using NLog;
using NLog.Config;
using NLog.Targets;
using SQLitePCL;
using Brushes = System.Drawing.Brushes;

namespace LabelVal;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    public static SimpleDatabase Settings { get; private set; }

#if DEBUG
    public static string WorkingDir => Directory.GetCurrentDirectory();
#else
        public static string WorkingDir { get; set; } =
 $"{Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)}\\LabelVal_Data";
#endif

    public static string Version { get; set; }

    public static string UserDataDirectory => $"{WorkingDir}\\UserData";
    public static string DatabaseExtension => ".sqlite";

    public static string SettingsDatabaseName => $"ApplicationSettings{DatabaseExtension}";

    public static string ImageResultsDatabaseDefaultName => "ImageResultsDatabase";

    public static string AssetsImageResultsDatabasesRoot =>
        $@"{Directory.GetCurrentDirectory()}\Assets\ImageResultsDatabases";

    public static string ImageResultsDatabaseRoot => $"{UserDataDirectory}\\ImageResultsDatabases";

    public static string AssetsImageRollRoot => $@"{Directory.GetCurrentDirectory()}\Assets\Image Rolls";
    public static string ImageRollsRoot => $"{UserDataDirectory}\\Image Rolls";
    public static string ImageRollsDatabasePath => $"{ImageRollsRoot}\\ImageRolls.sqlite";

    public static string RunsRoot => $"{UserDataDirectory}\\Runs";
    public static string RunLedgerDatabaseName => $"RunLedger{DatabaseExtension}";

    public static string RunResultsDatabaseName(long timeDate)
    {
        return $"Run_{timeDate}{DatabaseExtension}";
    }

    public App()
    {
        //   ExtractRunDetails();
        // File.WriteAllText("setting.imgr", JsonConvert.SerializeObject(new ImageRolls.ViewModels.ImageRollEntry(), new Newtonsoft.Json.Converters.StringEnumConverter()));
        SetupExceptionHandling();

        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
            Version = version.ToString();

        if (!Directory.Exists(UserDataDirectory))
            _ = Directory.CreateDirectory(UserDataDirectory);
        if (!Directory.Exists(ImageResultsDatabaseRoot))
            _ = Directory.CreateDirectory(ImageResultsDatabaseRoot);
        if (!Directory.Exists(ImageRollsRoot))
            _ = Directory.CreateDirectory(ImageRollsRoot);

        if (!Directory.Exists(RunsRoot))
            _ = Directory.CreateDirectory(RunsRoot);

        var config = new LoggingConfiguration();
        // Targets where to log to: File and Console
        var logfile = new FileTarget("logfile")
        {
            FileName = Path.Combine(UserDataDirectory, "log.txt"),
            ArchiveFileName = Path.Combine(UserDataDirectory, "log.${shortdate}.txt"),
            ArchiveAboveSize = 5242880,
            ArchiveEvery = FileArchivePeriod.Day,
            ArchiveNumbering = ArchiveNumberingMode.Rolling,
            MaxArchiveFiles = 3
        };
        config.AddRuleForAllLevels(logfile);
        LogManager.Configuration = config;

        LogManager.GetCurrentClassLogger().Info($"Starting: {Version}");

        try
        {
            Batteries.Init();
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex);
            Shutdown();
            return;
        }

        Settings = new SimpleDatabase().Open(Path.Combine(UserDataDirectory, SettingsDatabaseName));

        if (Settings == null)
        {
            LogManager.GetCurrentClassLogger().Error("The ApplicationSettings database is null. Shutdown!");
            Shutdown();
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        //RedrawFiducial(@"D:\OneDrive - OMRON\Omron\OCR\Applications\LabelVal\LabelVal\Assets\Standards\VALIDATION\300\FINAL 300dpi TEST ROLL 4x4 50labels_49a.png");
        ChangeColorBlindTheme(Settings.GetValue("App.IsColorBlind", false));

        // Set the application theme to Dark.Green
        _ = ThemeManager.Current.ChangeTheme(this, Settings.GetValue("App.Theme", "Dark.Steel"));

        ThemeManager.Current.ThemeChanged += Current_ThemeChanged;
        ThemeManager.Current.ThemeSyncMode = ThemeSyncMode.SyncWithAppMode;
    }

    private void Current_ThemeChanged(object sender, ThemeChangedEventArgs e)
    {
        Settings.SetValue("App.Theme", e.NewTheme.Name);
    }

    public static void ChangeColorBlindTheme(bool isColorBlind)
    {
        Settings.SetValue("App.IsColorBlind", isColorBlind);

        Current.Resources["CB_Green"] = isColorBlind
            ? Current.Resources["ColorBlindBrush1"]
            : new SolidColorBrush(Colors.Green);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        Settings?.Dispose();
    }

    private void SetupExceptionHandling()
    {
        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            LogUnhandledException((Exception)e.ExceptionObject, "AppDomain.CurrentDomain.UnhandledException", true);

        DispatcherUnhandledException += (s, e) =>
        {
            LogUnhandledException(e.Exception, "Application.Current.DispatcherUnhandledException", false);
            e.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (s, e) =>
        {
            LogUnhandledException(e.Exception, "TaskScheduler.UnobservedTaskException", false);
            e.SetObserved();
        };
    }

    private void LogUnhandledException(Exception exception, string source, bool shutdown)
    {
        var message = $"Unhandled exception ({source})";
        try
        {
            //NLog.LogManager.GetCurrentClassLogger().Error(exception);

            var assemblyName = Assembly.GetExecutingAssembly().GetName();
            message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
        }
        catch (Exception ex)
        {
            LogManager.GetCurrentClassLogger().Error(ex, "Exception in LogUnhandledException");
        }
        finally
        {
            LogManager.GetCurrentClassLogger().Error(exception, message);
        }

        _ = MessageBox.Show($"{message}\r\n{exception.Message}", "Unhandled Exception!", MessageBoxButton.OK);

        if (shutdown)
            Current.Dispatcher.Invoke(Shutdown);
    }


    private void ConvertDatabases()
    {
    }


    private void FixFiducial()
    {
        foreach (var dir in Directory.EnumerateDirectories(AssetsImageRollRoot))
            if (Directory.Exists($"{dir}\\300"))
                foreach (var imgFile in Directory.EnumerateFiles($"{dir}\\300"))
                    if (Path.GetExtension(imgFile) == ".png")
                        RedrawFiducial(imgFile);
    }

    private void RedrawFiducial(string path)
    {
        // load your photo
        using (var fs = new FileStream(path, FileMode.Open))
        {
            var photo = (Bitmap)Image.FromStream(fs);
            fs.Close();
            var newmap = new Bitmap(photo.Width, photo.Height);
            newmap.SetResolution(photo.HorizontalResolution, photo.VerticalResolution);
            //if (photo.Height != 2400)
            //    File.AppendAllText($"{UserDataDirectory}\\Small Images List", Path.GetFileName(path));

            //600 DPI
            //if ((photo.Height > 2400 && photo.Height != 4800) || photo.Height < 2000)
            //    return;

            //300 DPI
            if (photo.Height > 1200 || photo.Height < 1000)
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
        foreach (var dir in Directory.EnumerateDirectories(AssetsImageRollRoot))
        foreach (var imgFile in Directory.EnumerateFiles($"{dir}\\600"))
            if (imgFile.Contains("PRINT QUALITY"))
                RotateImage(imgFile);
    }

    private void RotateImage(string path)
    {
        // load your photo
        using (var fs = new FileStream(path, FileMode.Open))
        {
            var photo = Image.FromStream(fs);
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
    //private void ExtractRunDetails()
    //{
    //    var db = new Run.Database().Open(@"C:\Users\Jack\GitHub\LabelVal\LabelVal\bin\Debug\_p-000334 rev. 2_RunLog_Run10.db");
    //    var entries = db.SelectAllRunEntries().OrderBy(v => v.cycleID).ToList();

    //    var final = new FinalReport();

    //    var first = true;
    //    foreach (var entry in entries)
    //    {
    //        var report = Newtonsoft.Json.JsonConvert.DeserializeObject<Report>(entry.reportData);

    //        if (first)
    //        {
    //            final.LogName = "p-000334 rev. 2_RunLog_Run10";
    //            final.TemplateName = "p-000334 rev. 2";
    //            final.Operator = "epalacio";
    //            final.StartTime = entry.timeStamp;
    //            final.EndTime = entries.Last().timeStamp;
    //            final.Inspected = entries.Count;
    //            first = false;
    //        }

    //        if (report.inspectLabel.result == "pass")
    //        {
    //            final.GoodAccepted++;
    //        }
    //        else
    //        {
    //            final.Failed++;
    //            if (report.inspectLabel.userAction.action == "accepted")
    //                final.FailedAccepted++;
    //            else if (report.inspectLabel.userAction.action == "removed")
    //                final.Removed++;
    //            else if (report.inspectLabel.userAction.action == "voided")
    //                final.Voided++;
    //        }
    //    }


    //    File.WriteAllText("result.json", Newtonsoft.Json.JsonConvert.SerializeObject(final));
    //}

    // create an image of the desired size

    // save image to file or stream

    //}
}