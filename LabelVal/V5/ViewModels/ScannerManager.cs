using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using Org.BouncyCastle.Crypto.Prng;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace LabelVal.V5.ViewModels;

public partial class ScannerManager : ObservableObject
{
    public ObservableCollection<Scanner> Scanners { get; } = App.Settings.GetValue(nameof(Scanners), new ObservableCollection<Scanner>(), true);

    [ObservableProperty] private Scanner selectedScanner;

    [ObservableProperty] private Scanner newScanner;

    partial void OnSelectedScannerChanged(Scanner oldValue, Scanner newValue) => _ = WeakReferenceMessenger.Default.Send(new ScannerMessages.SelectedScannerChanged(newValue, oldValue));

    [RelayCommand] private void Add() => NewScanner = new Scanner();
    [RelayCommand] public void Cancel() => NewScanner = null;

    [RelayCommand]
    private void Save()
    {
        if(newScanner != null)
            Scanners.Add(newScanner);

        NewScanner = null;

        App.Settings.SetValue(nameof(Scanners), Scanners);
    }

    [RelayCommand] private void RemoveScanner(Scanner scanner) { Scanners.Remove(scanner); Save(); } 
    
}
