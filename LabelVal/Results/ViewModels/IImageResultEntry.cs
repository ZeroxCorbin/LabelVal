using LabelVal.ImageRolls.ViewModels;
using LabelVal.Sectors.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;

public interface IImageResultEntry
{
    ImageEntry SourceImage { get; }

    //V275
    ImageEntry V275CurrentImage { get; }
    DrawingImage V275CurrentImageOverlay { get; }
    ObservableCollection<ISector> V275CurrentSectors { get; }
    ISector V275FocusedCurrentSector { get; set; }

    ImageEntry V275StoredImage { get; }
    DrawingImage V275StoredImageOverlay { get; }
    ObservableCollection<ISector> V275StoredSectors { get; }
    ISector V275FocusedStoredSector { get; set; }

    ObservableCollection<ISectorDifferences> V275DiffSectors { get; }

    //V5
    ImageEntry V5CurrentImage { get; }
    DrawingImage V5CurrentImageOverlay { get; }
    ObservableCollection<ISector> V5CurrentSectors { get; }
    ISector V5FocusedCurrentSector { get; set; }

    ImageEntry V5StoredImage { get; }
    DrawingImage V5StoredImageOverlay { get; }
    ObservableCollection<ISector> V5StoredSectors { get; }
    ISector V5FocusedStoredSector { get; set; }

    ObservableCollection<ISectorDifferences> V5DiffSectors { get; }

    //L95xx
    ObservableCollection<ISector> L95xxCurrentSectors { get; }
    ISector L95xxFocusedCurrentSector { get; set; }

    ObservableCollection<ISector> L95xxStoredSectors { get; }
    ISector L95xxFocusedStoredSector { get; set; }

    ObservableCollection<ISectorDifferences> L95xxDiffSectors { get; }
}