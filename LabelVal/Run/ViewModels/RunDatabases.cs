using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Run.Databases;
using System.Collections.ObjectModel;

namespace LabelVal.Run.ViewModels;
public partial class RunDatabases : ObservableRecipient
{
    public ObservableCollection<RunDatabase> RunDatabasesList { get; } = [];
    [ObservableProperty][NotifyPropertyChangedRecipients] private RunDatabase selectedRunDatabase;

    public ObservableCollection<RunEntry> RunEntriesList { get; } = [];
    [ObservableProperty][NotifyPropertyChangedRecipients] private RunEntry selectedRunEntry;

    public RunDatabases() { IsActive = true; RefreshAll(); }

    public void LoadRunDatabases()
    {
        //Load RunDatabases from .sqlite files from the App.RunsRoot directory
        RunDatabasesList.Clear();
        foreach (string file in System.IO.Directory.GetFiles(App.RunsRoot, "*.sqlite"))
            RunDatabasesList.Add(new RunDatabase(file));
    }

    public void LoadRunEntries()
    {
        //get RunEntry from databases in RunDatabasesListe and put them in an RunEntriesList
        RunEntriesList.Clear();
        foreach (RunDatabase runDatabase in RunDatabasesList)
            foreach (RunEntry runEntry in runDatabase.SelectAllRunEntries())
                RunEntriesList.Add(runEntry);
    }

    [RelayCommand]
    private void RefreshAll()
    {
        LoadRunDatabases();
        LoadRunEntries();
    }
}
