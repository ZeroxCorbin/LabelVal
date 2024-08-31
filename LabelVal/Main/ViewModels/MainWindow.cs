using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using RingBuffer;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace LabelVal.Main.ViewModels;

public class DPIChangedMessage : ValueChangedMessage<DpiScale> { public DPIChangedMessage(DpiScale value) : base(value) { } }

public partial class MainWindow : ObservableRecipient, IRecipient<SystemMessages.StatusMessage>, IRecipient<V5_REST_Lib.Messages.LoggerMessage>, IRecipient<V275_REST_Lib.Messages.LoggerMessage>
{
    public static string Version => App.Version;

    [ObservableProperty] private DPIChangedMessage dPIChangedMessage;
    partial void OnDPIChangedMessageChanged(DPIChangedMessage value) => _ = WeakReferenceMessenger.Default.Send(value);

    public RingBufferCollection<SystemMessages.StatusMessage> SystemMessages_InfoDebug { get; } = new RingBufferCollection<SystemMessages.StatusMessage>(30);
    public RingBufferCollection<SystemMessages.StatusMessage> SystemMessages_ErrorWarning { get; } = new RingBufferCollection<SystemMessages.StatusMessage>(10);
    public SystemMessages.StatusMessage SystemMessages_RecentError => SystemMessages_ErrorWarning.Count > 0 ? SystemMessages_ErrorWarning[SystemMessages_ErrorWarning.Head] : null;

    public ObservableCollection<HamburgerMenuItem> MenuItems { get; }
    [ObservableProperty] private HamburgerMenuItem selectedMenuItem;

    public V275.ViewModels.V275Manager V275Manager { get; }
    public V275.ViewModels.NodeDetails NodeDetails { get; }

    public Printer.ViewModels.Printer Printer { get; }
    public Printer.ViewModels.PrinterDetails PrinterDetails { get; }

    public ImageRolls.ViewModels.ImageRolls ImageRolls { get; }
    public Results.ViewModels.ImageResults ImageResults { get; }

    public V5.ViewModels.ScannerManager ScannerManager { get; }
    public V5.ViewModels.ScannerDetails ScannerDetails { get; }

    public Run.ViewModels.RunManager RunManager { get; }

    public LVS_95xx.ViewModels.VerifierManager VerifierManager { get; }

    public Results.ViewModels.ImageResultsDatabases ImageResultsDatabases { get; }

    public List<string> Languages { get; } = ["English", "Español"];
    [ObservableProperty] private string selectedLanguage = App.Settings.GetValue(nameof(SelectedLanguage), "English", true);
    partial void OnSelectedLanguageChanged(string value)
    {
        Localization.TranslationSource.Instance.CurrentCulture = CultureInfo.GetCultureInfo(Localization.Culture.GetCulture(value));
        App.Settings.SetValue(nameof(SelectedLanguage), value);
    }

    public MainWindow()
    {
        Localization.TranslationSource.Instance.CurrentCulture = CultureInfo.GetCultureInfo(Localization.Culture.GetCulture(selectedLanguage));

        IsActive = true;

        //Should be loaded first to allow the property changed events to be received
        NodeDetails = new V275.ViewModels.NodeDetails();
        PrinterDetails = new Printer.ViewModels.PrinterDetails();
        ScannerDetails = new V5.ViewModels.ScannerDetails();

        Printer = new Printer.ViewModels.Printer();
        V275Manager = new V275.ViewModels.V275Manager();
        ScannerManager = new V5.ViewModels.ScannerManager();
        VerifierManager = new LVS_95xx.ViewModels.VerifierManager();

        ImageRolls = new ImageRolls.ViewModels.ImageRolls();
        ImageResultsDatabases = new Results.ViewModels.ImageResultsDatabases();
        ImageResults = new Results.ViewModels.ImageResults();

        RunManager = new Run.ViewModels.RunManager(ImageResults);

        MenuItems =
        [
            new HamburgerMenuItem { Label = "Printer", Content = Printer, IsNotSelectable = true },
            new HamburgerMenuItem { Label = "Run", Content = RunManager },
            new HamburgerMenuItem { Label = "Results", Content = ImageResultsDatabases },

            new HamburgerMenuItem { Label = "V275", Content = V275Manager },
            new HamburgerMenuItem { Label = "V5", Content = ScannerManager },
            new HamburgerMenuItem { Label = "L95xx", Content = VerifierManager },
        ];

        SelectedMenuItem = MenuItems[0];
    }

    public void Receive(SystemMessages.StatusMessage message)
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            App.Current.Dispatcher.BeginInvoke(() => Receive(message));
            return;
        }

        switch (message.Value)
        {
            case SystemMessages.StatusMessageType.Error:
                SystemMessages_ErrorWarning.Add(message);
                OnPropertyChanged(nameof(SystemMessages_RecentError));
                break;
            case SystemMessages.StatusMessageType.Warning:
                SystemMessages_ErrorWarning.Add(message);
                OnPropertyChanged(nameof(SystemMessages_RecentError));
                break;
            case SystemMessages.StatusMessageType.Debug:
                SystemMessages_InfoDebug.Add(message);
                break;
            case SystemMessages.StatusMessageType.Info:
                SystemMessages_InfoDebug.Add(message);
                break;
        }
    }

    public void Receive(V5_REST_Lib.Messages.LoggerMessage message)
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            App.Current.Dispatcher.BeginInvoke(() => Receive(message));
            return;
        }

        switch (message.Value) {
            case V5_REST_Lib.Messages.LoggerMessageTypes.Error:
                SystemMessages_ErrorWarning.Add(new SystemMessages.StatusMessage(message.Message, SystemMessages.StatusMessageType.Error));
                OnPropertyChanged(nameof(SystemMessages_RecentError));
                break;
            case V5_REST_Lib.Messages.LoggerMessageTypes.Warning:
                SystemMessages_ErrorWarning.Add(new SystemMessages.StatusMessage(message.Message, SystemMessages.StatusMessageType.Warning));
                OnPropertyChanged(nameof(SystemMessages_RecentError));
                break;
            case V5_REST_Lib.Messages.LoggerMessageTypes.Debug:
                SystemMessages_InfoDebug.Add(new SystemMessages.StatusMessage(message.Message, SystemMessages.StatusMessageType.Debug));
                break;
            case V5_REST_Lib.Messages.LoggerMessageTypes.Info:
                SystemMessages_InfoDebug.Add(new SystemMessages.StatusMessage(message.Message, SystemMessages.StatusMessageType.Info));
                break;
        }
    }

    public void Receive(V275_REST_Lib.Messages.LoggerMessage message)
    {
        if (!App.Current.Dispatcher.CheckAccess())
        {
            App.Current.Dispatcher.BeginInvoke(() => Receive(message));
            return; 
        }

        switch (message.Value)
        {
            case V275_REST_Lib.Messages.LoggerMessageTypes.Error:
                SystemMessages_ErrorWarning.Add(new SystemMessages.StatusMessage(message.Message, SystemMessages.StatusMessageType.Error));
                OnPropertyChanged(nameof(SystemMessages_RecentError));
                break;
            case V275_REST_Lib.Messages.LoggerMessageTypes.Warning:
                SystemMessages_ErrorWarning.Add(new SystemMessages.StatusMessage(message.Message, SystemMessages.StatusMessageType.Warning));
                OnPropertyChanged(nameof(SystemMessages_RecentError));
                break;
            case V275_REST_Lib.Messages.LoggerMessageTypes.Debug:
                SystemMessages_InfoDebug.Add(new SystemMessages.StatusMessage(message.Message, SystemMessages.StatusMessageType.Debug));
                break;
            case V275_REST_Lib.Messages.LoggerMessageTypes.Info:
                SystemMessages_InfoDebug.Add(new SystemMessages.StatusMessage(message.Message, SystemMessages.StatusMessageType.Info));
                break;
        }
    }
}
