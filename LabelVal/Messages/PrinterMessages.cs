using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Drawing.Printing;

namespace LabelVal.Messages;

public class PrinterMessages
{
    public class SelectedPrinterChanged(PrinterSettings newPrinter) : ValueChangedMessage<PrinterSettings>(newPrinter)
    {
    }
}
