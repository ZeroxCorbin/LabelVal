using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Main.Messages;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LabelVal.Main.ViewModels;

public partial class SplashScreenViewModel : ObservableRecipient, IRecipient<SplashScreenMessage>, IRecipient<CloseSplashScreenMessage>
{
    [ObservableProperty]
    private string _statusMessage = "Starting...";

    private readonly Queue<string> _messageQueue = new();
    private DispatcherTimer? _messageTimer;

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
        _messageQueue.Enqueue(message.Value);

        if (_messageTimer == null)
        {
            // Initialize and start the timer on the splash screen's dispatcher.
            _messageTimer = new DispatcherTimer(
                TimeSpan.FromMilliseconds(500),
                DispatcherPriority.Normal,
                ProcessMessageQueue,
                SplashScreenDispatcher ?? Dispatcher.CurrentDispatcher);
        }
        else if (!_messageTimer.IsEnabled)
        {
            _messageTimer.Start();
        }

        // If the splash screen is just starting, display the first message immediately.
        if (StatusMessage == "Starting...")
        {
            ProcessMessageQueue(this, EventArgs.Empty);
        }
    }

    private void ProcessMessageQueue(object? sender, EventArgs e)
    {
        if (_messageQueue.Count > 0)
        {
            StatusMessage = _messageQueue.Dequeue();
        }
        else
        {
            _messageTimer?.Stop();
        }
    }

    public async void Receive(CloseSplashScreenMessage message)
    {
        // Stop the timer on the correct dispatcher to prevent further message processing.
        SplashScreenDispatcher?.Invoke(() => _messageTimer?.Stop());

        // This message is sent from the main thread, so this method executes on the main thread.
        // We must use the cached SplashScreenDispatcher to invoke the close action.
        if (!message.NoDelay)
            await Task.Delay(2000);

        // Use the splash screen's dispatcher to invoke the close action.
        SplashScreenDispatcher?.Invoke(() => RequestClose?.Invoke());
    }
}