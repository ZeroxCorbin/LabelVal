using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Drawing.Printing;

namespace LabelVal.Printer.ViewModels;
public partial class PrinterDetails : ObservableRecipient, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    [ObservableProperty] private PrinterSettings selectedPrinter;
    public PrinterDetails() => IsActive = true;
    public void Receive(PropertyChangedMessage<PrinterSettings> message) => SelectedPrinter = message.NewValue;
}

