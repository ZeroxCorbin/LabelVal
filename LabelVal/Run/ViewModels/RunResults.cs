using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Results.Databases;
using LabelVal.Run.Databases;
using System;
using System.Collections.ObjectModel;

namespace LabelVal.Run.ViewModels;
public partial class RunResults : ObservableRecipient, IRecipient<PropertyChangedMessage<RunEntry>>, IRecipient<PropertyChangedMessage<RunDatabase>>
{
    ObservableCollection<RunResult> ImageResultsList { get; } = [];

    [ObservableProperty] private RunEntry selectedRunEntry;
    [ObservableProperty] private RunDatabase selectedDatabase;

    public RunResults() => IsActive = true;

    //if the selected run entry is changed. all CurrentImageResultGroup and StoredImageResultGroup entries should be loaded for the new SelectedDatabase.
    //The loaded entries should be added to the ImageResultsList as new RunResult objects.
    partial void OnSelectedRunEntryChanged(RunEntry value)
    {
        ImageResultsList.Clear();

        if (value == null) return;
        if (SelectedDatabase == null) return;

        foreach (var stored in SelectedDatabase.SelectAllStoredImageResultGroups(value.UID))
        {
            LogDebug($"Loading StoredImageResultGroup {stored.RunUID} {stored.SourceImageUID}");

            var current = SelectedDatabase.SelectCurrentImageResultGroup(stored.RunUID, stored.SourceImageUID);
            if (current != null)
                ImageResultsList.Add(new RunResult(current, stored));
            else
                LogError($"CurrentImageResultGroup not found for {stored.RunUID} and {stored.SourceImageUID}");
        }

    }

    #region Recieve Messages
    public void Receive(PropertyChangedMessage<RunEntry> message) => SelectedRunEntry = message.NewValue;
    public void Receive(PropertyChangedMessage<RunDatabase> message) => SelectedDatabase = message.NewValue;
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
