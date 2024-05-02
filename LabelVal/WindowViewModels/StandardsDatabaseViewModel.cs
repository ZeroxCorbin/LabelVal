using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Models;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LabelVal.Databases;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Text.RegularExpressions;
using MahApps.Metro.Controls.Dialogs;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;

namespace LabelVal.WindowViewModels;
public partial class StandardsDatabaseViewModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public MainWindowViewModel MainWindow => App.Current.MainWindow.DataContext as MainWindowViewModel;

    public class StandardsDBFile
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }

    private MainWindowViewModel MainWindowViewModel { get; }

    public StandardsDatabase StandardsDatabase { get; private set; }

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

    public bool IsDatabaseLocked
    {
        get => isDatabaseLocked || isDatabasePermLocked;
        set { _ = SetProperty(ref isDatabaseLocked, value); OnPropertyChanged("IsNotDatabaseLocked"); }
    }
    public bool IsNotDatabaseLocked => !isDatabaseLocked;
    private bool isDatabaseLocked = false;
    public bool IsDatabasePermLocked
    {
        get => isDatabasePermLocked;
        set { _ = SetProperty(ref isDatabasePermLocked, value); OnPropertyChanged("IsNotDatabasePermLocked"); }
    }
    public bool IsNotDatabasePermLocked => !isDatabasePermLocked;
    private bool isDatabasePermLocked = false;



    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;

    public StandardsDatabaseViewModel(MainWindowViewModel mainWindowViewModel)
    {
        MainWindowViewModel = mainWindowViewModel;

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

        StandardsDatabase?.Close();

        Logger.Info("Initializing standards database: {name}", file.FileName);
        StandardsDatabase = new StandardsDatabase(file.FilePath);

        var tables = StandardsDatabase.GetAllTables();

        IsDatabasePermLocked = tables.Contains("LOCKPERM");
        IsDatabaseLocked = tables.Contains("LOCK");

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
        _ = new StandardsDatabase(file.FilePath);

        SelectedStandardsDatabase = null;

        LoadStandardsDatabasesList();

        StoredStandardsDatabase = file;
        SelectStandardsDatabase();

    }

    [RelayCommand]
    private void LockStandardsDatabase()
    {
        if (IsDatabasePermLocked) return;

        if ((Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)) && (Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt)))
        {
            StandardsDatabase.DeleteLockTable(false);
            StandardsDatabase.CreateLockTable(true);
        }
        else
        {
            if (IsDatabaseLocked)
                StandardsDatabase.DeleteLockTable(false);
            else
                StandardsDatabase.CreateLockTable(false);
        }

        SelectedStandardsDatabase = null;
        SelectStandardsDatabase();
    }
}
