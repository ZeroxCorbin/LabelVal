using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.ObjectModel;

namespace LabelVal.LVS_95xx.ViewModels;
public partial class VerifierManager : ObservableRecipient
{
    public ObservableCollection<Verifier> Verifiers { get; } = App.Settings.GetValue(nameof(Verifiers), new ObservableCollection<Verifier>(), true);
    [ObservableProperty][NotifyPropertyChangedRecipients] public Verifier selectedVerifier;
    partial void OnSelectedVerifierChanged(Verifier value) => App.Settings.SetValue(nameof(SelectedVerifier), SelectedVerifier);
    
    [ObservableProperty] public Verifier newVerifier;

    public VerifierManager()
    {
        Verifier sel = App.Settings.GetValue<Verifier>(nameof(SelectedVerifier));

        foreach (Verifier verifier in Verifiers)
        {
            verifier.Manager = this;

            if (sel != null)
                if (verifier.ID == sel.ID)
                    SelectedVerifier = verifier;
        }

        WeakReferenceMessenger.Default.Register<RequestMessage<Verifier>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedVerifier);
            });
    }

    [RelayCommand]
    private void Add() => NewVerifier = new Verifier() { Manager = this };
    [RelayCommand]
    private void Cancel() => NewVerifier = null;
    [RelayCommand]
    private void Remove(Verifier scanner)
    {
        Verifiers.Remove(scanner);
        Save();
    }

    [RelayCommand]
    private void Save()
    {
        if (NewVerifier != null && NewVerifier != SelectedVerifier)
            Verifiers.Add(NewVerifier);

        NewVerifier = null;

        App.Settings.SetValue(nameof(Verifiers), Verifiers);
    }
}
