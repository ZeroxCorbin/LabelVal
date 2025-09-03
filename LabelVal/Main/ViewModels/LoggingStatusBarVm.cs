using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using RingBuffer.lib;

namespace LabelVal.Main.ViewModels;
public partial class LoggingStatusBarVm : ObservableRecipient, IRecipient<Logging.lib.LoggerMessage>
{
    public RingBufferCollection<Logging.lib.LoggerMessage> LoggerMessages { get; } = new RingBufferCollection<Logging.lib.LoggerMessage>(256);

    [ObservableProperty] private LoggerMessage? latest;
    [ObservableProperty] private LoggerMessage? latestError;
    [ObservableProperty] private LoggerMessage? latestWarning;
    [ObservableProperty] private LoggerMessage? latestInfo;
    [ObservableProperty] private LoggerMessage? latestDebug;

    public bool IsExpanded
    {
        get => App.Settings.GetValue("LoggingStatusBar.IsExpanded", false, true);
        set
        {
            App.Settings.SetValue("LoggingStatusBar.IsExpanded", value);
            OnPropertyChanged(nameof(IsExpanded));
        }
    }

    public LoggingStatusBarVm() => IsActive = true;
    public void Receive(LoggerMessage message)
    {
        switch (message?.Type)
        {
            case LoggerMessageTypes.Debug:
                LatestDebug = message;
                break;
            case LoggerMessageTypes.Info:
                LatestInfo = message;
                break;
            case LoggerMessageTypes.Warning:
                LatestWarning = message;
                break;
            case LoggerMessageTypes.Error:
                LatestError = message;
                break;
        }
        Latest = message;
        AddMessage(message);
    }

    private void AddMessage(LoggerMessage? message)
    {
        if (message == null)
            return;

        if (Application.Current != null && !Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => AddMessage(message));
            return;
        }
        LoggerMessages.Add(message);
    }

    [RelayCommand]
    private void ToggleIsExpanded()
    {
        IsExpanded = !IsExpanded;
    }

    [RelayCommand]

    public void Clear()
    {
        Latest = null;
        LatestDebug = null;
        LatestInfo = null;
        LatestWarning = null;
        LatestError = null;

        LoggerMessages.Clear();
    }
}