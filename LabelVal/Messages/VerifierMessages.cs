using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.LVS_95xx.ViewModels;

namespace LabelVal.Messages;

public class VerifierMessages
{
    public class SelectedVerifierChanged(Verifier newVerifier, Verifier oldVerifier) : ValueChangedMessage<Verifier>(newVerifier)
    {
        public Verifier OldVerifier { get; } = oldVerifier;
    }

    public class NewPacket(string packet) : ValueChangedMessage<string>(packet) { }

}
