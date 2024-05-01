using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Models;

namespace LabelVal.Messages;
public class StandardMessages
{
    public class SelectedStandardChanged(ImageRoll newStandard, ImageRoll oldStandard) : ValueChangedMessage<ImageRoll>(newStandard)
    {
        public ImageRoll OldStandard { get; } = oldStandard;
    }
}
