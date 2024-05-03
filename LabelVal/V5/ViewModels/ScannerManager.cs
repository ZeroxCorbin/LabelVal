using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using System.Collections.ObjectModel;

namespace LabelVal.V5.ViewModels;

public partial class ScannerManager : ObservableObject
{
    public ObservableCollection<Scanner> Scanners { get; } = App.Settings.GetValue(nameof(Scanners), new ObservableCollection<Scanner>(), true);

    [ObservableProperty] public Scanner selectedScanner;
    partial void OnSelectedScannerChanged(Scanner oldValue, Scanner newValue) => _ = WeakReferenceMessenger.Default.Send(new ScannerMessages.SelectedScannerChanged(newValue, oldValue));

    public ScannerManager()
    {
        if (Scanners.Count == 0)
        {
            Scanners.Add(new Scanner());
            SelectedScanner = Scanners[0];
        }
    }

    [RelayCommand] private void AddScanner() => Scanners.Add(new Scanner());
    [RelayCommand] private void RemoveScanner(Scanner scanner) => Scanners.Remove(scanner);
    [RelayCommand] private void SaveScanners() => App.Settings.SetValue(nameof(Scanners), Scanners);
}
