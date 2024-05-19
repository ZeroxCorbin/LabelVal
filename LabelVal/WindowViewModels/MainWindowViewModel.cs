using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using MahApps.Metro.Controls.Dialogs;
using System.Threading.Tasks;

namespace LabelVal.WindowViewModels;

public partial class MainWindowViewModel : ObservableRecipient, IRecipient<SystemMessages.StatusMessage>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    public static string Version => App.Version;

    public V275.ViewModels.V275 V275 { get; }
    public V275.ViewModels.NodeDetails NodeDetails { get; }

    public Printer.ViewModels.Printer Printer { get; }
    public Printer.ViewModels.PrinterDetails PrinterDetails { get; }

    public ImageRolls.ViewModels.ImageRolls ImageRolls { get; }
    public ImageRolls.ViewModels.ImageResults ImageResults { get; }

    public V5.ViewModels.ScannerManager ScannerManager { get; }
    public V5.ViewModels.ScannerDetails ScannerDetails { get; }

    public LVS_95xx.ViewModels.VerifierManager VerifierManager { get; }

    public ImageRolls.ViewModels.ImageResultsDatabases ImageResultsDatabases { get; }

    [ObservableProperty] private string userMessage = "";

    public static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public MainWindowViewModel()
    {
        IsActive = true;

        ImageResults = new ImageRolls.ViewModels.ImageResults();

        NodeDetails = new V275.ViewModels.NodeDetails();
        PrinterDetails = new Printer.ViewModels.PrinterDetails();
        ScannerDetails = new V5.ViewModels.ScannerDetails();

        V275 = new V275.ViewModels.V275();
        Printer = new Printer.ViewModels.Printer();
        VerifierManager = new LVS_95xx.ViewModels.VerifierManager();

        ImageRolls = new ImageRolls.ViewModels.ImageRolls(new System.Drawing.Printing.PrinterSettings() { PrinterName = Printer.SelectedPrinterName });
        ImageResultsDatabases = new ImageRolls.ViewModels.ImageResultsDatabases();

        ScannerManager = new V5.ViewModels.ScannerManager();

    }

    public void Receive(SystemMessages.StatusMessage message)
    {
        switch (message.Value)
        {
            case SystemMessages.StatusMessageType.Error:
                UserMessage = message.Message;
                break;
            case SystemMessages.StatusMessageType.Info:
                UserMessage = message.Message;
                break;
            case SystemMessages.StatusMessageType.Warning:
                UserMessage = message.Message;
                break;
            case SystemMessages.StatusMessageType.Control:
                break;
        }
    }


    public async Task OkDialog(string title, string message) => _ = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.Affirmative);
    public async Task<string> GetStringDialog(string title, string message) => await DialogCoordinator.ShowInputAsync(this, title, message);

    //private void UpdatePrinters()
    //{
    //    foreach (var r in Labels)
    //        r.PrinterName = StoredPrinter;

    //}

    //private async Task ResetRepeats()
    //{
    //    Repeats.Clear();

    //    await V275.Commands.GetRepeatsAvailable();

    //    if (V275.Commands.Available != null && V275.Commands.Available.Count > 0)
    //    {
    //        LabelCount = V275.Commands.Available[0];
    //        if (LabelCount == -1)
    //            LabelCount = 0;
    //    }

    //    else
    //        LabelCount = 0;
    //}

    [RelayCommand] private void ClearUserMessage() => UserMessage = "";

    //private void WebSocket_Heartbeat(V275_Events_System ev)
    //{
    //    string state = char.ToUpper(ev.data.state[0]) + ev.data.state.Substring(1);

    //    if (v275_State != state)
    //    {
    //        v275_State = state;

    //        OnPropertyChanged("V275_State");
    //        OnPropertyChanged("V275_JobName");
    //    }

    //}

}
