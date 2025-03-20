using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.GS1;
using BarcodeVerification.lib.ISO;
using LibSimpleDatabase;
using Lvs95xx.Producer.Watchers;
using MaterialDesignThemes.Wpf;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace LabelVal;

/// <summary>
///     Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
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

    //Load the parameters.csv file.
    //Trying to create a Dictionary<AvailableRegionType, List<AvailableParameters>> from the contents.
    //The first row is the header. the columns should match the AvailableRegionType enum by element name.
    //The remaining rows are the AvailableParameters that match the AvailableRegionType key.

    public class Regions
    {
        public static string GetUniqueList()
        {
            string filePath = "DeviceParameters.json";
            string jsonContent = File.ReadAllText(filePath);
            JObject jsonObject = JObject.Parse(jsonContent);

            foreach (JProperty category in jsonObject.Properties())
            {
                List<string> v275List = category.Value["V275"].ToObject<List<string>>();
                List<string> l95List = category.Value["L95"].ToObject<List<string>>();

                List<string> combinedList = v275List.Union(l95List).ToList();

                category.Value["V275"] = JArray.FromObject(combinedList);
                category.Value["L95"] = JArray.FromObject(combinedList);
            }
            string ret = jsonObject.ToString();
            return ret;
        }
        public static string LoadParameters()
        {
            using CsvHelper.CsvReader csv = new(new StreamReader("parameters.csv"), CultureInfo.InvariantCulture);
            _ = csv.Read();
            if (!csv.ReadHeader())
            {
                return "";
            }

            IEnumerable<dynamic> records = csv.GetRecords<dynamic>();

            Dictionary<AvailableRegionTypes, List<AvailableParameters>> results = [];

            foreach (dynamic record in records)
            {
                foreach (dynamic item in record)
                {
                    if (!Enum.TryParse<AvailableRegionTypes>(item.Key, out AvailableRegionTypes type))
                        continue;

                    if (string.IsNullOrWhiteSpace(item.Value))
                        continue;

                    string val = ConvertToCamelCase(item.Value);

                    if (!Enum.TryParse<AvailableParameters>(val, out AvailableParameters param))
                        continue;

                    if (!results.ContainsKey(type))
                        results[type] = [];

                    results[type].Add(param);

                    //var region = Enum.Parse<AvailableRegionTypes>(item.Key);
                    //var parameters = item.Value.Split(',').Select(v => Enum.Parse<AvailableParameters>(v)).ToList();
                    //RegionParameters.Add(region, parameters);
                }
            }

            List<AvailableParameters> commonAll = new();
            List<AvailableParameters> common1d = new();
            List<AvailableParameters> common2d = new();
            foreach (AvailableRegionTypes key in results.Keys)
            {
                results[key] = results[key].Distinct().ToList();

                //Check All common
                foreach (AvailableParameters param in results[key])
                {
                    if (results.Values.All(v => v.Contains(param)))
                    {
                        if (!commonAll.Contains(param))
                            commonAll.Add(param);
                    }
                }

                //Check 1D common
                foreach (AvailableParameters param in results[key])
                {
                    if (results[AvailableRegionTypes._1D].Contains(param)
                        && results[AvailableRegionTypes._1D1].Contains(param)
                        && results[AvailableRegionTypes._1D2].Contains(param)
                        && results[AvailableRegionTypes._1D3].Contains(param)
                        && results[AvailableRegionTypes._1D4].Contains(param)
                        && results[AvailableRegionTypes._1D5].Contains(param))
                    {
                        if (!common1d.Contains(param))
                            common1d.Add(param);
                    }
                }

                //Check 2D common
                foreach (AvailableParameters param in results[key])
                {
                    if (results[AvailableRegionTypes.DataMatrix].Contains(param)
                        && results[AvailableRegionTypes.QR].Contains(param)
                        && results[AvailableRegionTypes.QRMicro].Contains(param)
                        && results[AvailableRegionTypes.MaxiCode].Contains(param))
                    {
                        if (!common2d.Contains(param))
                            common2d.Add(param);
                    }
                }
            }

            foreach (AvailableRegionTypes key in results.Keys)
            {
                results[key] = results[key].Distinct().ToList();
            }

            return JsonConvert.SerializeObject(results);

        }

        //I would like to convert " a string with spaces " to "AStringWithSpaces"
        private static string ConvertToCamelCase(string value)
        {
            string[] parts = value.Split(' ');
            for (int i = 0; i < parts.Length; i++)
            {
                parts[i] = parts[i][..1].ToUpper() + parts[i][1..];
            }
            return string.Join("", parts);

        }
        //Symbology	Here is a list of supported symbologies: Code 39, ITF (I 2 of 5), Code 128, Codabar, GS1-128, UPC-A, UPC-E, EAN/JAN-8, EAN/JAN-13, DataBar-14 (linear), DataBar-stacked, DataBar-limited, DataBar-CCA, CCB, CCC, Pharmacode, PDF-417, Data Matrix ECC-200 (104x104 max), Data Matrix ECC-200 rectangular, PDF417, Micro-PDF417, QRCode, Aztec Code.
        //Xdim The nominal size of the narrow element(1x) in the symbol.

        public static void GetComments()
        {
            //open the file Descriptions.txt and for (each line) split the line into a key and a value seperated by a tab
            //There can be multiple values for a key, so the value should be a list of strings
            Dictionary<string, List<string>> comments = [];
            using StreamReader sr = new("Descriptions.txt");
            string line;

            while ((line = sr.ReadLine()) != null)
            {
                string[] parts = line.Split('\t');
                if (parts.Length != 2)
                    continue;

                if (!comments.ContainsKey(parts[0]))
                {

                    comments[parts[0]] = [];
                    comments[parts[0]].Add(parts[1]);
                    continue;
                }

                bool found = false;

                var str2 = parts[1].Trim();
                foreach (string comment in comments[parts[0]].ToArray())
                {
                   var str1 = comment.Trim();
                    
                    // Compare the strings
                    bool areEqual = str1.Equals(str2, StringComparison.Ordinal);
                    if (areEqual)
                    {
                        found = true;
                        break;
                    }

                }

                if (!found)
                    comments[parts[0]].Add(parts[1]);
            }

            StringBuilder sb = new();
            foreach (string key in comments.Keys)
            {
                foreach (string value in comments[key])
                {
                    sb.AppendLine($"{key}\t{value}");
                }
            }
            File.WriteAllText("Comments.txt", sb.ToString());
        }
    }





    public App()
    {
        SetupExceptionHandling();

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

        NLog.Config.LoggingConfiguration config = new();
        // Targets where to log to: File and Console
        NLog.Targets.FileTarget logfile = new("logfile")
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

        if (Keyboard.IsKeyDown(Key.LeftCtrl))
        {
            NLog.LogManager.GetCurrentClassLogger().Info($"CTRL Key pressed. Deleting contents of {WorkingDir}");
            RecursiveDelete(new DirectoryInfo(WorkingDir));
        }

        Settings = new SimpleDatabase();
        if (!Settings.Open(Path.Combine(UserDataDirectory, SettingsDatabaseName)))
        {
            Logger.LogError("The ApplicationSettings database is null. Shutdown!");
            Shutdown();
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        //ConvertToIndexedPNG();

        Logger.LogInfo($"Starting: Getting colorblind setting.");
        ChangeColorBlindTheme(Settings.GetValue("App.IsColorBlind", false));

        Logger.LogInfo($"Starting: Getting color theme.");
        string res = Settings.GetValue("App.Theme", "Dark.Steel", true);
        if (res.Contains("#"))
            ControlzEx.Theming.ThemeManager.Current.SyncTheme(ControlzEx.Theming.ThemeSyncMode.SyncAll);
        else
            _ = ControlzEx.Theming.ThemeManager.Current.ChangeTheme(this, res);

        UpdateMaterialDesignTheme();

        ControlzEx.Theming.ThemeManager.Current.ThemeChanged += Current_ThemeChanged;

        Logger.LogInfo($"Starting: Complete");
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
        try
        {
            Logger.LogError(exception, source);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Nested Exception in LogUnhandledException");
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