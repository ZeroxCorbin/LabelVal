using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.LVS_95xx.ViewModels;
public partial class VerifierManager : ObservableRecipient
{
    public ObservableCollection<Verifier> Verifiers { get; } = App.Settings.GetValue(nameof(Verifiers), new ObservableCollection<Verifier>(), true);

    [ObservableProperty][NotifyPropertyChangedRecipients] public Verifier selectedVerifier;

    public VerifierManager()
    {
        if (Verifiers.Count == 0)
        {
            Verifiers.Add(new Verifier());
            SelectedVerifier = Verifiers[0];
        }

        WeakReferenceMessenger.Default.Register<RequestMessage<Verifier>>(
            this,
            (recipient, message) =>
            {
                message.Reply(SelectedVerifier);
            });
    }

    [RelayCommand] private void AddVerifier() => Verifiers.Add(new Verifier());
    [RelayCommand] private void RemoveVerifier(Verifier scanner) => Verifiers.Remove(scanner);
    [RelayCommand] private void SaveVerifiers() => App.Settings.SetValue(nameof(Verifiers), Verifiers);

}
