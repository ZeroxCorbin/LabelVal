using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Sectors.ViewModels;

namespace LabelVal.Messages;

public class SectorMessages
{
    public class SelectedSectorChanged(Sector newSector) : ValueChangedMessage<Sector>(newSector)
    {
    }
}
