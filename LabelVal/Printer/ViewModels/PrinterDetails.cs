using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using System.Drawing.Printing;

namespace LabelVal.Printer.ViewModels;
public partial class PrinterDetails : ObservableRecipient, IRecipient<PrinterMessages.SelectedPrinterChanged>
{
    [ObservableProperty] private PrinterSettings selectedPrinter;
    public PrinterDetails() => IsActive = true;
    public void Receive(PrinterMessages.SelectedPrinterChanged message) => SelectedPrinter = message.Value;
}

