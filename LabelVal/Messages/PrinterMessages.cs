using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Drawing.Printing;

namespace LabelVal.Messages;

public class PrinterMessages
{
    public class SelectedPrinterChanged(PrinterSettings newPrinter, PrinterSettings oldPrinter) : ValueChangedMessage<PrinterSettings>(newPrinter)
    {
        public PrinterSettings OldPrinter { get; } = oldPrinter;
    }
}
