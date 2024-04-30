using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;

namespace LabelVal.Printer;
public partial class ViewModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public ObservableCollection<PrinterSettings> Printers { get; } = [];


    [ObservableProperty] private PrinterSettings selectedPrinter;
    partial void OnSelectedPrinterChanged(PrinterSettings value) => SelectedPrinterName = value.PrinterName;

    private string SelectedPrinterName { get => App.Settings.GetValue(nameof(SelectedPrinterName), ""); set => App.Settings.SetValue(nameof(SelectedPrinterName), value); }

    public ViewModel() => LoadPrinters();

    private void LoadPrinters()
    {
        Logger.Info("Loading printers.");

        foreach (string p in PrinterSettings.InstalledPrinters)
        {
            PrinterSettings printerSettings = new PrinterSettings { PrinterName = p };

            Printers.Add(printerSettings);
        }

        Logger.Info("Processed {count} printers.", Printers.Count);

        SelectedPrinter = Printers.FirstOrDefault(p => p.PrinterName == SelectedPrinterName);

        if (SelectedPrinter == null && Printers.Count > 0)
            SelectedPrinter = Printers.First();

    }

}
