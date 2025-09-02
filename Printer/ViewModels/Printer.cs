using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;

namespace LabelVal.Printer.ViewModels;
public partial class Printer : ObservableRecipient
{
    

    public ObservableCollection<string> Printers { get; } = [];


    [ObservableProperty][NotifyPropertyChangedRecipients] private PrinterSettings selectedPrinter;
    [ObservableProperty] private string selectedPrinterName;
    partial void OnSelectedPrinterNameChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            SelectedPrinter = new PrinterSettings() { PrinterName = value };
            App.Settings.SetValue(nameof(SelectedPrinterName), value);
        }
        else
            SelectedPrinter = null;
    }

    [ObservableProperty] private int printCount = App.Settings.GetValue<int>(nameof(PrintCount), 1, true);
    partial void OnPrintCountChanged(int value) => App.Settings.SetValue(nameof(PrintCount), value);

    public Printer()
    {
        WeakReferenceMessenger.Default.Register<RequestMessage<PrinterSettings>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedPrinter);
            });

        LoadPrinters();
    }

    private void LoadPrinters()
    {
        Printers.Clear();

        Logger.Info("Loading printers.");

        foreach (string p in PrinterSettings.InstalledPrinters)
            Printers.Add(p);

        Logger.Info($"Processed {Printers.Count} printers.");

        if (Printers.Contains(App.Settings.GetValue<string>(nameof(SelectedPrinterName))))
            SelectedPrinterName = App.Settings.GetValue<string>(nameof(SelectedPrinterName));
        else
        {
            if (Printers.Count == 0)
                Logger.Warning("No printers found.");
            else
            {
                Logger.Warning("Selected printer not found. Defaulting to first printer.");
                SelectedPrinterName = Printers.First();
            }

        }


    }

}
