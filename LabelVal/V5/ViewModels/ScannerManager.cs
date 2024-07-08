using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using NHibernate.Util;
using Org.BouncyCastle.Crypto.Prng;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using static System.ComponentModel.Design.ObjectSelectorEditor;

namespace LabelVal.V5.ViewModels;

public partial class ScannerManager : ObservableRecipient
{
    public ObservableCollection<Scanner> Scanners { get; } = App.Settings.GetValue(nameof(Scanners), new ObservableCollection<Scanner>(), true);

    [ObservableProperty][NotifyPropertyChangedRecipients] private Scanner selectedScanner;

    [ObservableProperty] private Scanner newScanner;

    public ScannerManager()
    {
        foreach (var scanner in Scanners) 
            scanner.Manager = this;

        if (selectedScanner == null)
            SelectedScanner = Scanners.FirstOrDefault();

        WeakReferenceMessenger.Default.Register<RequestMessage<Scanner>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedScanner);
            });
    }

    [RelayCommand] private void Add() => NewScanner = new Scanner() { Manager = this };
    [RelayCommand] public void Cancel() => NewScanner = null;

    [RelayCommand]
    private void Save()
    {
        if(NewScanner != null)
            Scanners.Add(NewScanner);

        NewScanner = null;

        App.Settings.SetValue(nameof(Scanners), Scanners);
    }

    [RelayCommand] private void Remove(Scanner scanner) { Scanners.Remove(scanner); Save(); } 
    
}
