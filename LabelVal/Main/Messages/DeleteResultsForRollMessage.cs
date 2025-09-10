using CommunityToolkit.Mvvm.Messaging.Messages;

namespace LabelVal.Main.Messages;

public class DeleteResultsForRollMessage : ValueChangedMessage<string>
{
    public DeleteResultsForRollMessage(string rollUid) : base(rollUid) { }
}