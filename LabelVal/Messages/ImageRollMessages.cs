using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;

namespace LabelVal.Messages;
public class ImageRollMessages
{
    public class SelectedImageRollChanged(ImageRoll newStandard, ImageRoll oldStandard) : ValueChangedMessage<ImageRoll>(newStandard)
    {
        public ImageRoll OldStandard { get; } = oldStandard;
    }
}
