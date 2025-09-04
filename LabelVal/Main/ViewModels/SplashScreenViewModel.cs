using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Main.Messages;
using System;
using System.Threading.Tasks;

namespace LabelVal.Main.ViewModels;

public partial class SplashScreenViewModel : ObservableRecipient, IRecipient<SplashScreenMessage>, IRecipient<CloseSplashScreenMessage>
{
    [ObservableProperty]
    private string _statusMessage = "Starting...";

    public Action? RequestClose { get; set; }

    public SplashScreenViewModel()
    {
        IsActive = true;
    }

    public void Receive(SplashScreenMessage message)
    {
        StatusMessage = message.Value;
    }

    public void Receive(CloseSplashScreenMessage message)
    {
        Task.Run(() => { System.Threading.Thread.Sleep(2000); App.Current.Dispatcher.Invoke(() => RequestClose?.Invoke()); });
        
    }
}