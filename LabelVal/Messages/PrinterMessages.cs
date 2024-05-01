using CommunityToolkit.Mvvm.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Messages
{
    public class PrinterMessages
    {
        public class SelectedPrinterChanged(PrinterSettings newPrinter, PrinterSettings oldPrinter) : ValueChangedMessage<PrinterSettings>(newPrinter)
        {
            public PrinterSettings OldPrinter { get; } = oldPrinter;
        }
    }
}
