using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.Databases;

namespace LabelVal.Messages;
public class DatabaseMessages
{
    public class SelectedDatabseChanged(ImageResults newDatabase, ImageResults oldDatabase) : ValueChangedMessage<ImageResults>(newDatabase)
    {
        public ImageResults OldDatabase { get; } = oldDatabase;
    }
}
