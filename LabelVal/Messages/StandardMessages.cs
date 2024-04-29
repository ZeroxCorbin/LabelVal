using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Models;

namespace LabelVal.Messages;
public class StandardMessages
{
    public class SelectedStandardChanged(StandardEntryModel newStandard, StandardEntryModel oldStandard) : ValueChangedMessage<StandardEntryModel>(newStandard)
    {
        public StandardEntryModel OldStandard { get; } = oldStandard;
    }
}
