using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Messages;
using LabelVal.Results.Databases;
using LabelVal.Run.Databases;
using LabelVal.Sectors.ViewModels;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using LabelVal.WindowViewModels;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json;
using NHibernate.Util;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using V275_REST_lib.Models;

namespace LabelVal.Run.ViewModels;
public partial class ImageResults : ObservableRecipient,
    IRecipient<PropertyChangedMessage<RunEntry>>,
    IRecipient<PropertyChangedMessage<RunDatabase>>, 
    IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    private int PrintCount => App.Settings.GetValue<int>(nameof(PrintCount));
    private int LoopCount => App.Settings.GetValue(nameof(LoopCount), 1);

    public ObservableCollection<ImageResultGroup> ImageResultsList { get; } = [];
    [ObservableProperty] private ImageResultGroup selectedImageResultGroup;

    [ObservableProperty] private RunDatabase selectedDatabase;
    [ObservableProperty] private RunEntry selectedRunEntry;
    [ObservableProperty] private PrinterSettings selectedPrinter;

    public ImageResults() => IsActive = true;



    #region Recieve Messages    
    public void Receive(PropertyChangedMessage<RunEntry> message) => SelectedRunEntry = message.NewValue;
    public void Receive(PropertyChangedMessage<RunDatabase> message) => SelectedDatabase = message.NewValue;
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
