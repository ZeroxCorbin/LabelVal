using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.ObjectModel;

namespace LabelVal.V5.ViewModels;

public partial class ScannerManager : ObservableRecipient
{
    public ObservableCollection<Scanner> Scanners { get; } = App.Settings.GetValue(nameof(Scanners), new ObservableCollection<Scanner>(), true);
    [ObservableProperty][NotifyPropertyChangedRecipients] private Scanner selectedScanner;
    partial void OnSelectedScannerChanged(Scanner value) => App.Settings.SetValue(nameof(SelectedScanner), value);

    [ObservableProperty] private Scanner newScanner;

    public ScannerManager()
    {
        Scanner sel = App.Settings.GetValue<Scanner>(nameof(SelectedScanner));

        foreach (Scanner scanner in Scanners)
        {
            scanner.Manager = this;

            if (sel != null)
                if (scanner.ID == sel.ID)
                    SelectedScanner = scanner;
        }

        WeakReferenceMessenger.Default.Register<RequestMessage<Scanner>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedScanner);
            });
    }

    [RelayCommand]
    private void Add() => NewScanner = new Scanner() { Manager = this };
    [RelayCommand]
    private void Edit() => NewScanner = SelectedScanner;
    [RelayCommand]
    private void Cancel() => NewScanner = null;
    [RelayCommand]
    private void Delete()
    {
        Scanners.Remove(NewScanner);
        NewScanner = null;
        Save();
    }
    [RelayCommand]
    private void Save()
    {
        if (NewScanner != null && NewScanner != SelectedScanner)
            Scanners.Add(NewScanner);

        NewScanner = null;

        App.Settings.SetValue(nameof(Scanners), Scanners);
    }
}
