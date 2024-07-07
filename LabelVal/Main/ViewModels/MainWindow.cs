using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using MahApps.Metro.Controls.Dialogs;
using RingBuffer;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal.Main.ViewModels
{
    public class DPIChangedMessage : ValueChangedMessage<DpiScale> { public DPIChangedMessage(DpiScale value) : base(value) { } }

    public partial class MainWindow : ObservableRecipient, IRecipient<SystemMessages.StatusMessage>
    {
        public static string Version => App.Version;

        [ObservableProperty] private DPIChangedMessage dPIChangedMessage;
        partial void OnDPIChangedMessageChanged(DPIChangedMessage value) => _ = WeakReferenceMessenger.Default.Send(value);

        public RingBufferCollection<SystemMessages.StatusMessage> SystemMessages_InfoDebug { get; } = new RingBufferCollection<SystemMessages.StatusMessage>(30);
        public RingBufferCollection<SystemMessages.StatusMessage> SystemMessages_ErrorWarning { get; } = new RingBufferCollection<SystemMessages.StatusMessage>(10);
        public SystemMessages.StatusMessage SystemMessages_RecentError => SystemMessages_ErrorWarning.Count > 0 ? SystemMessages_ErrorWarning[SystemMessages_ErrorWarning.Head] : null;

        public V275.ViewModels.V275 V275 { get; }
        public V275.ViewModels.NodeDetails NodeDetails { get; }

        public Printer.ViewModels.Printer Printer { get; }
        public Printer.ViewModels.PrinterDetails PrinterDetails { get; }

        public ImageRolls.ViewModels.ImageRolls ImageRolls { get; }
        public Results.ViewModels.ImageResults ImageResults { get; }

        public V5.ViewModels.ScannerManager ScannerManager { get; }
        public V5.ViewModels.ScannerDetails ScannerDetails { get; }

        public Run.ViewModels.RunControl RunControl { get; }


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

            ImageResults = new Results.ViewModels.ImageResults();

            NodeDetails = new V275.ViewModels.NodeDetails();
            PrinterDetails = new Printer.ViewModels.PrinterDetails();
            ScannerDetails = new V5.ViewModels.ScannerDetails();

            V275 = new V275.ViewModels.V275();
            Printer = new Printer.ViewModels.Printer();
            VerifierManager = new LVS_95xx.ViewModels.VerifierManager();

            ImageRolls = new ImageRolls.ViewModels.ImageRolls(new System.Drawing.Printing.PrinterSettings() { PrinterName = Printer.SelectedPrinterName });
            ImageResultsDatabases = new Results.ViewModels.ImageResultsDatabases();

            ScannerManager = new V5.ViewModels.ScannerManager();
        }

        public void Receive(SystemMessages.StatusMessage message)
        {
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

        #region Dialogs

        public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
        public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);
        public async Task<string> GetStringDialog(string title, string message) => await DialogCoordinator.ShowInputAsync(this, title, message);

        #endregion
    }
}
