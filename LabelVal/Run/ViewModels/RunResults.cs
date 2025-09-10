using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Results.Databases;
using LabelVal.Run.Databases;
using System;
using System.Collections.ObjectModel;
using System.Drawing.Printing;

namespace LabelVal.Run.ViewModels;
public partial class RunResults : ObservableRecipient, IRecipient<PropertyChangedMessage<RunEntry>>
{
    public ObservableCollection<RunResult> RunResultsList { get; } = [];
    [ObservableProperty] private RunEntry selectedRunEntry;

    public RunResults() => IsActive = true;

    //if the selected run entry is changed. all CurrentResultsGroup and StoredResultsGroup entries should be loaded for the new SelectedDatabase.
    //The loaded entries should be added to the ResultssEntries as new RunResult objects.
    partial void OnSelectedRunEntryChanged(RunEntry value)
    {
        RunResultsList.Clear();

        if (value == null) return;
        if (SelectedRunEntry == null) return;
        //var vals = SelectedRunEntry.RunDatabase.SelectAllStoredResultsGroups(value.UID);
        foreach (var stored in SelectedRunEntry.ResultsDatabase.SelectAllResultsGroups(value.UID))
        {
            //Logger.Debug($"Loading StoredResultsGroup {stored.RunUID} {stored.SourceImageUID}");

            //var current = SelectedRunEntry.RunDatabase.SelectCurrentResultsGroup(stored.RunUID, stored.SourceImageUID, stored.Order);

            //if (current != null)
            //    RunResultsList.Add(new RunResult(current, stored, value));
            //else
            //    Logger.Error($"CurrentResultsGroup not found for {stored.RunUID} and {stored.SourceImageUID}");
        }

    }

    #region Receive Messages
    public void Receive(PropertyChangedMessage<RunEntry> message) => SelectedRunEntry = message.NewValue;
    #endregion

}
