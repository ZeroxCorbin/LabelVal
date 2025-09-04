using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Main.Messages;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LabelVal.Main.ViewModels;

public partial class SplashScreenViewModel : ObservableRecipient, IRecipient<SplashScreenMessage>, IRecipient<CloseSplashScreenMessage>
{
    [ObservableProperty]
    private string _statusMessage = "Starting...";

    public Action? RequestClose { get; set; }

    // Store the dispatcher for the splash screen's thread
    public Dispatcher? SplashScreenDispatcher { get; set; }

    public SplashScreenViewModel()
    {
        IsActive = true;
    }

    public void Receive(SplashScreenMessage message)
    {
        // This message is sent from the main thread via App.UpdateSplashScreen,
        // which correctly dispatches it to the splash screen's thread.
        // So, this update happens on the correct thread.
        StatusMessage = message.Value;
    }

    public async void Receive(CloseSplashScreenMessage message)
    {
        // This message is sent from the main thread, so this method executes on the main thread.
        // We must use the cached SplashScreenDispatcher to invoke the close action.
        await Task.Delay(2000);

        // Use the splash screen's dispatcher to invoke the close action.
        SplashScreenDispatcher?.Invoke(() => RequestClose?.Invoke());
    }
}