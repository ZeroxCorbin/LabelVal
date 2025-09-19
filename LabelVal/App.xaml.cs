using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Main.Messages;
using LabelVal.Main.Views;
using LabelVal.Sectors.Classes;
using LabelVal.Theme;
using LibSimpleDatabase;
using Lvs95xx.Producer.Watchers;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace LabelVal;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private static readonly IHost _host = Host
    .CreateDefaultBuilder()
    .ConfigureAppConfiguration(c =>
    {
        if (Assembly.GetEntryAssembly()?.Location is string loc)
            if (Path.GetDirectoryName(loc) is string dir)
                c.SetBasePath(dir);
    })
    .ConfigureServices((context, services) =>
    {
        services.AddSingleton<IMessenger, WeakReferenceMessenger>(provider =>
        {
            return WeakReferenceMessenger.Default;
        });

        _ = services.AddSingleton<SectorDifferencesDatabaseSettings>(provider =>
        { 
            return App.Settings.GetValue<SectorDifferencesDatabaseSettings>(nameof(SectorDifferencesDatabaseSettings), new(), true) ?? new SectorDifferencesDatabaseSettings();
        });

}).Build();

    public static T GetService<T>() where T : notnull => _host.Services.GetRequiredService<T>();

    public static SimpleDatabase Settings { get; private set; }

    public static GS1Encoder GS1Encoder = new();

    public static ActiveWatchers Watchers { get; } = new ActiveWatchers();

    private static Main.Views.SplashScreen _splashScreen;

    private static bool _showSplashScreen = true;
    public static bool ShowSplashScreen
    {
        get=> _showSplashScreen;
        set
        {
            _showSplashScreen = value;

            if (value)
                DisplaySplashScreen();
        }
    }

#if DEBUG
    public static string WorkingDir => Directory.GetCurrentDirectory();
#else
    public static string WorkingDir => Directory.GetCurrentDirectory();
#endif

    public static string Version { get; set; }

    public static string UserDataDirectory => $"{WorkingDir}\\UserData";
    public static string DatabaseExtension => ".sqlite";

    public static string SettingsDatabaseName => $"ApplicationSettings{DatabaseExtension}";

    public static string ResultssDatabaseDefaultName => "ResultssDatabase";
    public static string DisplaysDatabaseName => $"Displays{DatabaseExtension}";

    public static string ResultssDatabaseRoot => $@"{UserDataDirectory}\Image Results";

    public static string AssetsImageRollsRoot => $@"{Directory.GetCurrentDirectory()}\Assets\Image Rolls";
    public static string UserImageRollsRoot => $"{UserDataDirectory}\\Image Rolls";
    public static string UserImageRollDefaultFile => $"{UserImageRollsRoot}\\ImageRolls.sqlite";

    public static string RunsRoot => $"{UserDataDirectory}\\Runs";
    public static string RunLedgerDatabaseName => $"RunLedger{DatabaseExtension}";

    public static string RunResultsDatabaseName(long timeDate) => $"Run_{timeDate}{DatabaseExtension}";

    private static ManualResetEvent _splashScreenReady = new(false);
    private static Dispatcher? _splashScreenDispatcher;


    public App()
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        if (version != null)
            Version = version.ToString();

        if (!Directory.Exists(UserDataDirectory))
            _ = Directory.CreateDirectory(UserDataDirectory);

        if (!Directory.Exists(ResultssDatabaseRoot))
            _ = Directory.CreateDirectory(ResultssDatabaseRoot);

        if (!Directory.Exists(UserImageRollsRoot))
            _ = Directory.CreateDirectory(UserImageRollsRoot);

        if (!Directory.Exists(RunsRoot))
            _ = Directory.CreateDirectory(RunsRoot);

        NLog.Config.LoggingConfiguration config = new();
        NLog.Targets.FileTarget logfile = new("logfile")
        {
            FileName = Path.Combine(UserDataDirectory, "log.txt"),
            ArchiveFileName = Path.Combine(UserDataDirectory, "log.${shortdate}.txt"),
            ArchiveAboveSize = 5242880,
            ArchiveEvery = NLog.Targets.FileArchivePeriod.Day,
            ArchiveSuffixFormat = "yyyy-MM-dd",
            MaxArchiveFiles = 3
        };
        config.AddRuleForAllLevels(logfile);
        NLog.LogManager.Configuration = config;

        NLog.LogManager.GetCurrentClassLogger().Info($"Starting: {Version}");

        if (Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            NLog.LogManager.GetCurrentClassLogger().Info($"CTRL Key pressed. Deleting contents of {WorkingDir}");
            RecursiveDelete(new DirectoryInfo(WorkingDir));
        }

        Settings = new SimpleDatabase();
        if (!Settings.Open(Path.Combine(UserDataDirectory, SettingsDatabaseName)))
        {
            Logger.Error("The ApplicationSettings database is null. Shutdown!");
            Shutdown();
        }

    }

    protected override void OnStartup(StartupEventArgs e)
    {  
        DisplaySplashScreen();
        
        _host.Start();

        base.OnStartup(e);

        // Wait until the splash screen is created and its dispatcher is running
        _splashScreenReady.WaitOne();

        UpdateSplashScreen("Loading settings...");

        Logger.Info("Starting: Getting colorblind setting.");
        var colorBlindnessType = Settings.GetValue("App.ColorBlindnessType", ColorBlindnessType.None);
        Dispatcher.Invoke(() => ThemeSupport.ApplyColorBlindTheme(colorBlindnessType));

        UpdateSplashScreen("Initializing main window...");

        // Defer non-critical UI updates until the application is idle.
        // This allows the main window to render sooner.
        _ = Dispatcher.InvokeAsync(async () =>
        {
            await Task.Run(() =>
            {
                UpdateSplashScreen("Applying themes...");
                Logger.Info("Starting: Getting color theme.");
                var themeName = Settings.GetValue("App.Theme", "Dark.Steel", true);

                // Migrate any legacy '#' values that are not the sentinel.
                if (themeName.Contains("#", StringComparison.Ordinal) && themeName != ThemeSupport.SystemSyncSentinel)
                    themeName = ThemeSupport.SystemSyncSentinel;

                Dispatcher.Invoke(() =>
                {
                    // Centralized apply (covers OS sync or explicit theme)
                    ThemeSupport.ApplyTheme(themeName);

                    UpdateMaterialDesignTheme();

                    ControlzEx.Theming.ThemeManager.Current.ThemeChanged += Current_ThemeChanged;
                });
            });

            Logger.Info("Starting: Complete");

            _ = Application.Current.Dispatcher.BeginInvoke(() =>
            {
                _ = WeakReferenceMessenger.Default.Send(new SplashScreenMessage("Loading Main Window..."));
            });

            var mainWindow = new MainWindow();
            this.MainWindow = mainWindow;

            mainWindow.Show();            

        }, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
    }

    private static void DisplaySplashScreen()
    {
        Thread splashThread = new(() => Display())
        {
            IsBackground = true,
            Name = "SplashScreenThread"
        };
        splashThread.SetApartmentState(ApartmentState.STA); // Splash screen needs to be STA
        splashThread.Start();
    }

    private static void Display()
    {
        if (_splashScreen != null)
            return;

        _splashScreen = new Main.Views.SplashScreen();
        if (_splashScreen.DataContext is Main.ViewModels.SplashScreenViewModel vm)
        {
            vm.SplashScreenDispatcher = Dispatcher.CurrentDispatcher;
            vm.RequestClose = () => CloseSplashScreen();
        }
        _splashScreen.Show();

        _splashScreenDispatcher = Dispatcher.CurrentDispatcher;
        _splashScreenReady.Set(); // Signal that the splash screen is ready

        // Start the dispatcher processing loop
        Dispatcher.Run();

        _splashScreen = null;
    }

    public static void UpdateSplashScreen(string message)
    {
        _splashScreenDispatcher?.BeginInvoke(
            (Action)(() => WeakReferenceMessenger.Default.Send(new SplashScreenMessage(message)))
        );
    }

    public static void CloseSplashScreen()
    {
        if(_splashScreen == null)
            return;

        if(_splashScreen.DataContext is Main.ViewModels.SplashScreenViewModel vm)
        {
            vm.IsActive = false;
        }
        _splashScreenDispatcher?.BeginInvokeShutdown(DispatcherPriority.Normal);
        //App.Current.Dispatcher.BeginInvoke(() => App.Current.MainWindow.BringIntoView());
    }
    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);

        Settings?.Dispose();
    }

    private void Current_ThemeChanged(object sender, ControlzEx.Theming.ThemeChangedEventArgs e)
    {
        // If user chose OS sync, keep sentinel instead of overwriting with the resolved concrete theme name.
        if (Settings.GetValue("App.Theme", ThemeSupport.SystemSyncSentinel, true) != ThemeSupport.SystemSyncSentinel)
            Settings.SetValue("App.Theme", e.NewTheme.Name);

        UpdateMaterialDesignTheme();
    }
    public static void ChangeColorBlindTheme(ColorBlindnessType colorBlindnessType)
    {
        Settings.SetValue("App.ColorBlindnessType", colorBlindnessType);

        string suffix = colorBlindnessType switch
        {
            ColorBlindnessType.RedGreen => "_RG",
            ColorBlindnessType.BlueYellow => "_BY",
            ColorBlindnessType.Monochrome => "_M",
            _ => ""
        };

        var resourceKeys = new[]
        {
            "ISO_GradeA","ISO_GradeB","ISO_GradeC","ISO_GradeD","ISO_GradeF",
            "SectorWarning","SectorError",
            "StatusGreen","StatusYellow","StatusRed",
            "V275","V5","L95","ImageRoll","Results","LabelBuilder",
            // New app brand keys
            "AppPrimary","AppSecondary","AppAccent","AppInfo","AppNeutral"
        };

        foreach (var key in resourceKeys)
        {
            var baseBrush = Current.Resources[$"{key}{suffix}_Brush"];
            var baseColor = (System.Windows.Media.Color)Current.Resources[$"{key}{suffix}"];

            Current.Resources[$"{key}_Brush_Active"] = baseBrush;
            Current.Resources[$"{key}_Brush_Active50"] =
                new SolidColorBrush(System.Windows.Media.Color.FromArgb(164, baseColor.R, baseColor.G, baseColor.B));
            Current.Resources[$"{key}_Color_Active"] = Current.Resources[$"{key}{suffix}"];
            Current.Resources[$"{key}_Color_Active50"] =
                System.Windows.Media.Color.FromArgb(164, baseColor.R, baseColor.G, baseColor.B);
        }
    }
    private void UpdateMaterialDesignTheme()
    {
        PaletteHelper hel = new();
        MaterialDesignThemes.Wpf.Theme theme = new();
        var the = ControlzEx.Theming.ThemeManager.Current.DetectTheme();

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
        try
        {
            Logger.Error(exception, source);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Nested Exception in LogUnhandledException");
        }

        if (shutdown)
        {
            _ = MessageBox.Show($"{exception.Message}", source, MessageBoxButton.OK);
            Current.Dispatcher.Invoke(Shutdown);
        }
    }

    public static void RecursiveDelete(DirectoryInfo baseDir)
    {
        if (!baseDir.Exists)
            return;

        foreach (var dir in baseDir.EnumerateDirectories())
        {
            if (dir.FullName.Contains("UserData"))
                continue;

            RecursiveDelete(dir);
        }
        var files = baseDir.GetFiles();
        foreach (var file in files)
        {
            file.IsReadOnly = false;
            file.Delete();
        }
        baseDir.Delete();
    }

    private void RedrawFiducial(string path)
    {
        // load your photo
        using FileStream fs = new(path, FileMode.Open);
        var photo = (Bitmap)Image.FromStream(fs);
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

        using var graphics = Graphics.FromImage(newmap);
        graphics.DrawImage(photo, 0, 0, photo.Width, photo.Height);
        //graphics.FillRectangle(Brushes.White, 0, 1900, 210, photo.Height - 1900);
        //graphics.FillRectangle(Brushes.Black, 30, 1950, 90, 90);

        //300 DPI
        graphics.FillRectangle(System.Drawing.Brushes.White, 0, 976, 150, photo.Height - 976);
        graphics.FillRectangle(System.Drawing.Brushes.Black, 15, 975, 45, 45);

        newmap.Save(path, ImageFormat.Png);
    }

    private void FixRotation()
    {
        foreach (var dir in Directory.EnumerateDirectories(AssetsImageRollsRoot))
            foreach (var imgFile in Directory.EnumerateFiles($"{dir}\\600"))
                if (imgFile.Contains("PRINT QUALITY"))
                    RotateImage(imgFile);
    }

    private void RotateImage(string path)
    {
        // load your photo
        using FileStream fs = new(path, FileMode.Open);
        var photo = Image.FromStream(fs);
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


    private void ExtractRunDetails()
    {
        var db = new V275_REST_Lib.LocalDatabases.RunLogDatabase().Open(@"impede_RunLog_FR25040802U CARTON_Run180.db");
        var entries = db.SelectAllRunEntries().OrderBy(v => v.cycleId).ToList();

        var final = new FinalReport();

        var first = true;
        foreach (var entry in entries)
        {
            var report = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(entry.reportData);

            if (first)
            {
                final.LogName = "impede_RunLog_FR25040802U CARTON_Run180.db";
                final.TemplateName = "FR25040802U CARTON";
                final.Operator = "PanC";
                final.StartTime = entry.timeStamp;
                final.EndTime = entries.Last().timeStamp;
                final.Inspected = entries.Count;
                first = false;
            }

            if (report["inspectLabel"]["result"].Value<string>() == "pass")
            {
                final.GoodAccepted++;
            }
            else
            {
                final.Failed++;

                var action = report["inspectLabel"]?["userAction"]?["action"]?.Value<string>();
                if (!string.IsNullOrEmpty(action))
                {
                    if (action == "accepted")
                        final.FailedAccepted++;
                    else if (action == "removed")
                        final.Removed++;
                    else if (action == "voided")
                        final.Voided++;
                }
                else
                {
                    var sectors = report["inspectLabel"]?["inspectSector"];
                    if (sectors == null)
                        continue;

                    var found = false;
                    foreach (var sec in sectors)
                    {
                        var alarms = sec["data"]?["alarms"];
                        if (alarms == null)
                            continue;

                        foreach (var alarm in alarms)
                        {
                            var userAction = alarm["userAction"];
                            if (userAction != null)
                            {
                                action = userAction["action"]?.Value<string>();
                                if (!string.IsNullOrEmpty(action))
                                {
                                    if (action == "accept")
                                        final.FailedAccepted++;
                                    else if (action == "remove")
                                        final.Removed++;
                                    else if (action == "void")
                                        final.Voided++;
                                    found = true;
                                    break;
                                }

                            }
                        }
                        if (found)
                            break;
                    }
                }

            }
        }

        File.WriteAllText("result.json", Newtonsoft.Json.JsonConvert.SerializeObject(final));
    }


}