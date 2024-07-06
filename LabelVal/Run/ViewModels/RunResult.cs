using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Results.Databases;
using LabelVal.Run.Databases;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Threading.Tasks;

namespace LabelVal.Run.ViewModels;
public partial class RunResult : ObservableRecipient,
    IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    private int PrintCount => App.Settings.GetValue<int>(nameof(PrintCount));
    private int LoopCount => App.Settings.GetValue(nameof(LoopCount), 1);

    public CurrentImageResultGroup CurrentImageResultGroup { get; } 
    public StoredImageResultGroup StoredImageResultGroup { get; } 

    [ObservableProperty] private PrinterSettings selectedPrinter;

    public RunResult() => IsActive = true;

    public RunResult(CurrentImageResultGroup current, StoredImageResultGroup stored)
    {
        CurrentImageResultGroup = current;
        StoredImageResultGroup = stored;

        IsActive = true;
    }

    #region Recieve Messages    
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
    #endregion

    #region Dialogs
    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public async Task<MessageDialogResult> OkCancelDialog(string title, string message) => await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);
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
