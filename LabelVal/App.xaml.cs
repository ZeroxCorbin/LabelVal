using BarcodeVerification.lib.GS1.Encoders;
using LibSimpleDatabase;
using Lvs95xx.Producer.Watchers;
using MaterialDesignThemes.Wpf;
using NLog;
using NLog.Config;
using NLog.Targets;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LabelVal;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static SimpleDatabase Settings { get; private set; }

    public static GS1Encoder GS1Encoder = new();

    public static ActiveWatchers Watchers { get; } = new ActiveWatchers();

#if DEBUG
    public static string WorkingDir => Directory.GetCurrentDirectory();
#else
        public static string WorkingDir { get; set; } = System.IO.Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);
#endif

    public static string Version { get; set; }
    //public static string LocalAppData => System.IO.Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), System.Reflection.Assembly.GetExecutingAssembly().GetName().Name);

    public static string UserDataDirectory => $"{WorkingDir}\\UserData";
    public static string DatabaseExtension => ".sqlite";

    public static string SettingsDatabaseName => $"ApplicationSettings{DatabaseExtension}";

    public static string ImageResultsDatabaseDefaultName => "ImageResultsDatabase";

    //public static string AssetsImageResultsDatabasesRoot => $@"{Directory.GetCurrentDirectory()}\Assets\ImageResultsDatabases";

    public static string ImageResultsDatabaseRoot => $@"{UserDataDirectory}\Image Results";

    public static string AssetsImageRollsRoot => $@"{Directory.GetCurrentDirectory()}\Assets\Image Rolls";
    public static string UserImageRollsRoot => $"{UserDataDirectory}\\Image Rolls";
    public static string UserImageRollDefaultFile => $"{UserImageRollsRoot}\\ImageRolls.sqlite";

    public static string RunsRoot => $"{UserDataDirectory}\\Runs";
    public static string RunLedgerDatabaseName => $"RunLedger{DatabaseExtension}";

    public static string RunResultsDatabaseName(long timeDate) => $"Run_{timeDate}{DatabaseExtension}";

    public App()
    {
        //SetupExceptionHandling();

        Version version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
            Version = version.ToString();

        if (!Directory.Exists(UserDataDirectory))
            _ = Directory.CreateDirectory(UserDataDirectory);

        if (!Directory.Exists(ImageResultsDatabaseRoot))
            _ = Directory.CreateDirectory(ImageResultsDatabaseRoot);

        if (!Directory.Exists(UserImageRollsRoot))
            _ = Directory.CreateDirectory(UserImageRollsRoot);

        if (!Directory.Exists(RunsRoot))
            _ = Directory.CreateDirectory(RunsRoot);

        LoggingConfiguration config = new();
        // Targets where to log to: File and Console
        FileTarget logfile = new("logfile")
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

        if (Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            NLog.LogManager.GetCurrentClassLogger().Info($"CTRL Key pressed. Deleting contents of {WorkingDir}");
            RecursiveDelete(new DirectoryInfo(WorkingDir));
        }

        //LogManager.GetCurrentClassLogger().Info($"Starting: Plugging in batteries.");
        //try
        //{
        //    Batteries.Init();
        //}
        //catch (Exception ex)
        //{
        //    LogManager.GetCurrentClassLogger().Error(ex);
        //    Shutdown();
        //    return;
        //}

        Settings = new SimpleDatabase();
        if (!Settings.Open(Path.Combine(UserDataDirectory, SettingsDatabaseName)))
        {
            LogManager.GetCurrentClassLogger().Error("The ApplicationSettings database is null. Shutdown!");
            Shutdown();
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        //ConvertToIndexedPNG();

        LogManager.GetCurrentClassLogger().Info($"Starting: Getting colorblind setting.");
        ChangeColorBlindTheme(Settings.GetValue("App.IsColorBlind", false));

        LogManager.GetCurrentClassLogger().Info($"Starting: Getting color theme.");
        string res = Settings.GetValue("App.Theme", "Dark.Steel", true);
        if (res.Contains("#"))
            ControlzEx.Theming.ThemeManager.Current.SyncTheme(ControlzEx.Theming.ThemeSyncMode.SyncAll);
        else
            _ = ControlzEx.Theming.ThemeManager.Current.ChangeTheme(this, res);

        UpdateMaterialDesignTheme();

        ControlzEx.Theming.ThemeManager.Current.ThemeChanged += Current_ThemeChanged;

        LogManager.GetCurrentClassLogger().Info($"Starting: Complete");
    }
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        Settings?.Dispose();
    }

    private void Current_ThemeChanged(object sender, ControlzEx.Theming.ThemeChangedEventArgs e)
    {
        Settings.SetValue("App.Theme", e.NewTheme.Name);
        UpdateMaterialDesignTheme();
    }
    public static void ChangeColorBlindTheme(bool isColorBlind)
    {
        Settings.SetValue("App.IsColorBlind", isColorBlind);

        Current.Resources["CB_Green"] = isColorBlind
            ? Current.Resources["ColorBlindBrush1"]
            : Current.Resources["ISO_GradeA_Brush"];
    }
    private void UpdateMaterialDesignTheme()
    {
        PaletteHelper hel = new();
        Theme theme = new();
        ControlzEx.Theming.Theme the = ControlzEx.Theming.ThemeManager.Current.DetectTheme();

        theme.SetPrimaryColor(the.PrimaryAccentColor);
        if (the.BaseColorScheme == ControlzEx.Theming.ThemeManager.BaseColorDark)
            theme.SetBaseTheme(BaseTheme.Dark);
        else
            theme.SetBaseTheme(BaseTheme.Light);
        hel.SetTheme(theme);
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
        string message = $"Unhandled exception ({source})";
        try
        {
            _ = MessageBox.Show($"{exception.Message}\r\n{exception.InnerException.Message}");
            Logger.Error(exception, message);

            AssemblyName assemblyName = Assembly.GetExecutingAssembly().GetName();
            message = string.Format("Unhandled exception in {0} v{1}", assemblyName.Name, assemblyName.Version);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Exception in LogUnhandledException");
        }
        finally
        {
            Logger.Error(exception, message);
        }

        if (shutdown)
        {
            _ = MessageBox.Show($"{message}\r\n{exception.Message}", "Unhandled Exception!", MessageBoxButton.OK);
            Current.Dispatcher.Invoke(Shutdown);
        }
    }

    public static void RecursiveDelete(DirectoryInfo baseDir)
    {
        if (!baseDir.Exists)
            return;

        foreach (DirectoryInfo dir in baseDir.EnumerateDirectories())
        {
            if (dir.FullName.Contains("UserData"))
                continue;

            RecursiveDelete(dir);
        }
        FileInfo[] files = baseDir.GetFiles();
        foreach (FileInfo file in files)
        {
            file.IsReadOnly = false;
            file.Delete();
        }
        baseDir.Delete();
    }

    private void ConvertToIndexedPNG()
    {
        DirectoryInfo outDir = Directory.CreateDirectory($"{UserDataDirectory}\\IndexedPNG");
        foreach (string dir in Directory.EnumerateDirectories(AssetsImageRollsRoot))
        {
            string dirName = Path.GetFileName(dir);
            DirectoryInfo imgDir;

            if (Directory.Exists($"{dir}\\600"))
            {
                imgDir = Directory.CreateDirectory($"{outDir.FullName}\\{dirName}\\600");
                foreach (string file in Directory.EnumerateFiles($"{dir}\\600"))
                {
                    string newFileName = $"{imgDir.FullName}\\{Path.GetFileName(file)}";
                    if (Path.GetExtension(file) is ".bmp" or ".png")
                        File.WriteAllBytes(newFileName, LibImageUtilities.ImageTypes.Png.Utilities.GetPng(File.ReadAllBytes(file), PixelFormat.Format8bppIndexed));
                    else
                        File.Copy(file, newFileName);
                }
            }

            if (Directory.Exists($"{dir}\\300"))
            {
                imgDir = Directory.CreateDirectory($"{outDir.FullName}\\{dirName}\\300");

                foreach (string file in Directory.EnumerateFiles($"{dir}\\300"))
                {
                    string newFileName = $"{imgDir.FullName}\\{Path.GetFileName(file)}";
                    if (Path.GetExtension(file) is ".bmp" or ".png")
                        File.WriteAllBytes(newFileName, LibImageUtilities.ImageTypes.Png.Utilities.GetPng(File.ReadAllBytes(file), PixelFormat.Format8bppIndexed));
                    else
                        File.Copy(file, newFileName);
                }
            }
        }
    }

    private void ConvertDatabases()
    {
    }

    private void FixFiducial()
    {
        foreach (string dir in Directory.EnumerateDirectories(AssetsImageRollsRoot))
            if (Directory.Exists($"{dir}\\300"))
                foreach (string imgFile in Directory.EnumerateFiles($"{dir}\\300"))
                    if (Path.GetExtension(imgFile) == ".png")
                        RedrawFiducial(imgFile);
    }

    private void RedrawFiducial(string path)
    {
        // load your photo
        using FileStream fs = new(path, FileMode.Open);
        Bitmap photo = (Bitmap)Image.FromStream(fs);
        fs.Close();
        Bitmap newmap = new(photo.Width, photo.Height);
        newmap.SetResolution(photo.HorizontalResolution, photo.VerticalResolution);
        //if (photo.Height != 2400)
        //    File.AppendAllText($"{UserDataDirectory}\\Small Images List", Path.GetFileName(path));

        //600 DPI
        //if ((photo.Height > 2400 && photo.Height != 4800) || photo.Height < 2000)
        //    return;

        //300 DPI
        if (photo.Height is > 1200 or < 1000)
            return;

        using Graphics graphics = Graphics.FromImage(newmap);
        graphics.DrawImage(photo, 0, 0, photo.Width, photo.Height);
        //graphics.FillRectangle(Brushes.White, 0, 1900, 210, photo.Height - 1900);
        //graphics.FillRectangle(Brushes.Black, 30, 1950, 90, 90);

        //300 DPI
        graphics.FillRectangle(Brushes.White, 0, 976, 150, photo.Height - 976);
        graphics.FillRectangle(Brushes.Black, 15, 975, 45, 45);

        newmap.Save(path, ImageFormat.Png);
    }

    private void FixRotation()
    {
        foreach (string dir in Directory.EnumerateDirectories(AssetsImageRollsRoot))
            foreach (string imgFile in Directory.EnumerateFiles($"{dir}\\600"))
                if (imgFile.Contains("PRINT QUALITY"))
                    RotateImage(imgFile);
    }

    private void RotateImage(string path)
    {
        // load your photo
        using FileStream fs = new(path, FileMode.Open);
        Image photo = Image.FromStream(fs);
        fs.Close();

        photo.RotateFlip(RotateFlipType.Rotate180FlipNone);
        photo.Save(path, ImageFormat.Png);
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