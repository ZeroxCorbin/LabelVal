using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Run.Databases;
using System.Collections.ObjectModel;

namespace LabelVal.Run.ViewModels;
public partial class RunDatabases : ObservableRecipient
{
    public ObservableCollection<ResultsDatabase> RunDatabasesList { get; } = [];
    public ObservableCollection<RunEntry> RunEntriesList { get; } = [];
    [ObservableProperty][NotifyPropertyChangedRecipients] private RunEntry selectedRunEntry;
    partial void OnSelectedRunEntryChanged(RunEntry value) { }

    public RunDatabases() { IsActive = true; RefreshAll(); }

    public void LoadRunDatabases()
    {
        //Load RunDatabases from .sqlite files from the App.RunsRoot directory
        RunDatabasesList.Clear();
        foreach (var file in System.IO.Directory.GetFiles(App.RunsRoot, "*.sqlite"))
            RunDatabasesList.Add(new ResultsDatabase(file));
    }

    public void LoadRunEntries()
    {
        //get RunEntry from databases in RunDatabasesListe and put them in an RunEntriesList
        RunEntriesList.Clear();
        foreach (var runDatabase in RunDatabasesList)
            foreach (var runEntry in runDatabase.SelectAllRunEntries())
            {
                runEntry.ResultsDatabase = runDatabase;
                RunEntriesList.Add(runEntry);
            }
    }

    [RelayCommand]
    private void RefreshAll()
    {
        LoadRunDatabases();
        LoadRunEntries();
    }
}
