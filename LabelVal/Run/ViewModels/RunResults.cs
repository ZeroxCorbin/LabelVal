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

    //if the selected run entry is changed. all CurrentImageResultGroup and StoredImageResultGroup entries should be loaded for the new SelectedDatabase.
    //The loaded entries should be added to the ImageResultsList as new RunResult objects.
    partial void OnSelectedRunEntryChanged(RunEntry value)
    {
        RunResultsList.Clear();

        if (value == null) return;
        if (SelectedRunEntry == null) return;
        //var vals = SelectedRunEntry.RunDatabase.SelectAllStoredImageResultGroups(value.UID);
        foreach (var stored in SelectedRunEntry.RunDatabase.SelectAllImageResultGroups(value.UID))
        {
            //LogDebug($"Loading StoredImageResultGroup {stored.RunUID} {stored.SourceImageUID}");

            //var current = SelectedRunEntry.RunDatabase.SelectCurrentImageResultGroup(stored.RunUID, stored.SourceImageUID, stored.Order);

            //if (current != null)
            //    RunResultsList.Add(new RunResult(current, stored, value));
            //else
            //    LogError($"CurrentImageResultGroup not found for {stored.RunUID} and {stored.SourceImageUID}");
        }

    }

    #region Recieve Messages
    public void Receive(PropertyChangedMessage<RunEntry> message) => SelectedRunEntry = message.NewValue;
    #endregion

    #region Logging
    private readonly Logging.Logger logger = new();
    public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
    public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
    public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
    public void LogError(string message) => logger.LogError(this.GetType(), message);
    public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
    public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
    #endregion
}
