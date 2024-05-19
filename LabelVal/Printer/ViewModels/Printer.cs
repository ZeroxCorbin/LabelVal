using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;

namespace LabelVal.Printer.ViewModels;
public partial class Printer : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public ObservableCollection<string> Printers { get; } = [];
    [ObservableProperty] private PrinterSettings selectedPrinter;
    partial void OnSelectedPrinterChanged(PrinterSettings value) => _ = WeakReferenceMessenger.Default.Send(new PrinterMessages.SelectedPrinterChanged(value));

    [ObservableProperty] private string selectedPrinterName;
    partial void OnSelectedPrinterNameChanged(string value)
    {
        if (!string.IsNullOrEmpty(value))
        {
            App.Settings.SetValue(nameof(SelectedPrinterName), value);
            SelectedPrinter = new PrinterSettings() { PrinterName = value };
        }
        else
            SelectedPrinter = null;
    }

    [ObservableProperty] private int printCount = App.Settings.GetValue<int>(nameof(PrintCount), 1, true);
    partial void OnPrintCountChanged(int value) => App.Settings.SetValue(nameof(PrintCount), value);

    public Printer() => LoadPrinters();

    private void LoadPrinters()
    {
        Printers.Clear();

        Logger.Info("Loading printers.");

        foreach (string p in PrinterSettings.InstalledPrinters)
            Printers.Add(p);

        Logger.Info("Processed {count} printers.", Printers.Count);

        if (Printers.Contains(App.Settings.GetValue<string>(nameof(SelectedPrinterName))))
            SelectedPrinterName = App.Settings.GetValue<string>(nameof(SelectedPrinterName));
        else
        {
            if (Printers.Count == 0)
                Logger.Warn("No printers found.");
            else
            {
                Logger.Warn("Selected printer not found. Defaulting to first printer.");
                SelectedPrinterName = Printers.First();
            }

        }


    }

}
