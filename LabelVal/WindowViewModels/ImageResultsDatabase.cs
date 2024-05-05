using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.ImageRolls.Databases;
using LabelVal.Messages;
using MahApps.Metro.Controls.Dialogs;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LabelVal.WindowViewModels;
public partial class ImageResultsDatabase : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public class StandardsDBFile
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }

    [ObservableProperty] private ImageResults selectedDatabase;
    partial void OnSelectedDatabaseChanged(ImageResults oldValue, ImageResults newValue) => _ = WeakReferenceMessenger.Default.Send(new DatabaseMessages.SelectedDatabseChanged(newValue, oldValue));

    public StandardsDBFile StoredStandardsDatabase { get => App.Settings.GetValue("StoredStandardsDatabase_1", new StandardsDBFile() { FilePath = Path.Combine(App.StandardsDatabaseRoot, App.StandardsDatabaseDefaultName + App.DatabaseExtension), FileName = App.StandardsDatabaseDefaultName }); set => App.Settings.SetValue("StoredStandardsDatabase_1", value); }
    public ObservableCollection<StandardsDBFile> StandardsDatabases { get; } = [];

    [ObservableProperty] private StandardsDBFile selectedStandardsDatabase;
    partial void OnSelectedStandardsDatabaseChanged(StandardsDBFile value)
    {
        // SelectedImageRoll = null;

        if (value != null)
        {
            StoredStandardsDatabase = value;

            LoadStandardsDatabase(StoredStandardsDatabase);
            //SelectImageRoll();
        }
    }

    private ObservableCollection<string> OrphandStandards { get; } = [];

    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public ImageResultsDatabase()
    {
        LoadStandardsDatabasesList();
        SelectStandardsDatabase();
    }

    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);
    public async Task<string> GetStringDialog(string title, string message) => await DialogCoordinator.ShowInputAsync(this, title, message);

    private void LoadStandardsDatabasesList()
    {
        Logger.Info("Loading grading standards databases from file system. {path}", App.StandardsDatabaseRoot);

        StandardsDatabases.Clear();
        SelectedStandardsDatabase = null;

        foreach (var file in Directory.EnumerateFiles(App.AssetsStandardsDatabasesRoot))
        {
            Logger.Debug("Found: {name}", Path.GetFileName(file));

            if (file.EndsWith(App.DatabaseExtension))
                StandardsDatabases.Add(new StandardsDBFile() { FileName = Path.GetFileName(file).Replace(App.DatabaseExtension, ""), FilePath = file });
        }

        foreach (var file in Directory.EnumerateFiles(App.StandardsDatabaseRoot))
        {
            Logger.Debug("Found: {name}", Path.GetFileName(file));

            if (file.EndsWith(App.DatabaseExtension))
                StandardsDatabases.Add(new StandardsDBFile() { FileName = Path.GetFileName(file).Replace(App.DatabaseExtension, ""), FilePath = file });
        }

        if (StandardsDatabases.Count == 0)
            StandardsDatabases.Add(new StandardsDBFile() { FilePath = Path.Combine(App.StandardsDatabaseRoot, App.StandardsDatabaseDefaultName + App.DatabaseExtension), FileName = App.StandardsDatabaseDefaultName });
    }
    private void SelectStandardsDatabase()
    {
        var res = StandardsDatabases.Where((a) => a.FilePath == StoredStandardsDatabase.FilePath);
        if (res.FirstOrDefault() != null)
            SelectedStandardsDatabase = res.FirstOrDefault();
        else if (StandardsDatabases.Count > 0)
            SelectedStandardsDatabase = StandardsDatabases.First();
    }
    private void LoadStandardsDatabase(StandardsDBFile file)
    {
        OrphandStandards.Clear();

        //string file = Path.Combine(App.StandardsDatabaseRoot, fileName + App.DatabaseExtension);

        //if (!File.Exists(file))
        //    file = Path.Combine(App.AssetsStandardsDatabasesRoot, fileName + App.DatabaseExtension);

        SelectedDatabase?.Close();

        Logger.Info("Initializing standards database: {name}", file.FileName);
        SelectedDatabase = new ImageResults();
        SelectedDatabase.Open(file.FilePath);

        //var tables = SelectedDatabase.GetAllTables();

        //foreach (var tbl in tables)
        //{
        //    ImageRoll std;
        //    if ((std = ImageRolls.FirstOrDefault((e) => e.Name.Equals(tbl))) == null)
        //    {
        //        if (tbl.StartsWith("LOCK"))
        //            continue;
        //        else
        //            OrphandStandards.Add(tbl);
        //    }
        //    //else
        //    //    std.NumRows = StandardsDatabase.GetAllRowsCount(tbl);
        //}
    }

    [RelayCommand]
    private async Task CreateStandardsDatabase()
    {
        var res = await GetStringDialog("New Standards Database", "What is the name of the new database?");
        if (res == null) return;

        if (string.IsNullOrEmpty(res) || res.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
        {
            _ = OkDialog("Invalid Name", $"The name '{res}' contains invalid characters.");
            return;
        }

        var file = new StandardsDBFile() { FilePath = Path.Combine(App.StandardsDatabaseRoot, res + App.DatabaseExtension), FileName = res };

        SelectedStandardsDatabase = null;
        LoadStandardsDatabasesList();

        StoredStandardsDatabase = file;
        SelectStandardsDatabase();

    }

    [RelayCommand]
    private void LockStandardsDatabase()
    {
        if (SelectedDatabase.IsPermLocked)
            return;

        //if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
        //{
        //    StandardsDatabase.DeleteLockTable(false);
        //    StandardsDatabase.CreateLockTable(true);
        //}
        //else
        //{
        SelectedDatabase.IsLocked = !SelectedDatabase.IsLocked;
        //}

    }
}
