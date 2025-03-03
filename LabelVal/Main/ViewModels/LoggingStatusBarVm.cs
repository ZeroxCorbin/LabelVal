using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using Logging.lib;
using RingBuffer.lib;

namespace LabelVal.Main.ViewModels;
public partial class LoggingStatusBarVm : ObservableRecipient, IRecipient<Logging.lib.LoggerMessage>
{
    public RingBufferCollection<Logging.lib.LoggerMessage> LoggerMessages { get; } = new RingBufferCollection<Logging.lib.LoggerMessage>(30);

    [ObservableProperty] private LoggerMessage? latestError;
    [ObservableProperty] private LoggerMessage? latestWarning;
    [ObservableProperty] private LoggerMessage? latestInfo;
    [ObservableProperty] private LoggerMessage? latestDebug;

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
        AddMessage(message);
    }

    private void AddMessage(LoggerMessage? message)
    {
        if (message == null)
            return;

        if (App.Current != null && !App.Current.Dispatcher.CheckAccess())
        {
            App.Current.Dispatcher.BeginInvoke(() => AddMessage(message));
            return;
        }
        LoggerMessages.Add(message);
    }

    [RelayCommand]

    public void Clear()
    {
        LatestDebug = null;
        LatestInfo = null;
        LatestWarning = null;
        LatestError = null;
    }
}
