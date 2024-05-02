using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Databases;

namespace LabelVal.Messages;
public class DatabaseMessages
{
    public class SelectedDatabseChanged(StandardsDatabase newDatabase, StandardsDatabase oldDatabase) : ValueChangedMessage<StandardsDatabase>(newDatabase)
    {
        public StandardsDatabase OldDatabase { get; } = oldDatabase;
    }
}
