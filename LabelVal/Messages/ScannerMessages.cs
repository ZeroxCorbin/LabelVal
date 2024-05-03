using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.V5.ViewModels;

namespace LabelVal.Messages;

public class ScannerMessages
{
    public class SelectedScannerChanged(Scanner newScanner, Scanner oldScanner) : ValueChangedMessage<Scanner>(newScanner)
    {
        public Scanner OldScanner { get; } = oldScanner;
    }
}
