using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.Linq;

namespace LabelVal.Printer;
public partial class ViewModel : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public ObservableCollection<string> Printers { get; } = [];

    [ObservableProperty] private string selectedPrinter = App.Settings.GetValue(nameof(SelectedPrinter), "");
    partial void OnSelectedPrinterChanged(string value) => App.Settings.SetValue(nameof(SelectedPrinter), value);

    public ViewModel() => LoadPrinters();

    private void LoadPrinters()
    {
        Logger.Info("Loading printers.");

        foreach (string p in PrinterSettings.InstalledPrinters)
        {
            Printers.Add(p);

            SelectedPrinter ??= p;
        }

        Logger.Info("Processed {count} printers.", Printers.Count);

        if (string.IsNullOrEmpty(SelectedPrinter) && Printers.Count > 0)
            SelectedPrinter = Printers.First();
    }

}
