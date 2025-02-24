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
    private void LogInfo(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
#if DEBUG
    private void LogDebug(string message) => Logging.lib.Logger.LogDebug(GetType(), message);
#else
    private void LogDebug(string message) { }
#endif
    private void LogWarning(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
    private void LogError(string message) => Logging.lib.Logger.LogError(GetType(), message);
    private void LogError(Exception ex) => Logging.lib.Logger.LogError(GetType(), ex);
    private void LogError(string message, Exception ex) => Logging.lib.Logger.LogError(GetType(), ex, message);

    #endregion
}
