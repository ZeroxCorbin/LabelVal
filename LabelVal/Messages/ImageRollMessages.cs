using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;

namespace LabelVal.Messages;
public class ImageRollMessages
{
    public class SelectedImageRollChanged(ImageRollEntry newStandard, ImageRollEntry oldStandard) : ValueChangedMessage<ImageRollEntry>(newStandard)
    {
        public ImageRollEntry OldStandard { get; } = oldStandard;
    }
}
