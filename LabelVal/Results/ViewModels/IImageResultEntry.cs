using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Sectors.ViewModels;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;

public interface IImageResultEntry
{
    ImageEntry SourceImage { get; }

    //V275
    ImageEntry V275CurrentImage { get; }
    DrawingImage V275CurrentImageOverlay { get; }
    ObservableCollection<Sector> V275CurrentSectors { get; }
    Sector V275FocusedCurrentSector { get; set; }

    ImageEntry V275StoredImage { get; }
    DrawingImage V275StoredImageOverlay { get; }
    ObservableCollection<Sector> V275StoredSectors { get; }
    Sector V275FocusedStoredSector { get; set; }

    ObservableCollection<SectorDifferences> V275DiffSectors { get; }

    //V5
    ImageEntry V5CurrentImage { get; }
    DrawingImage V5CurrentImageOverlay { get; }
    ObservableCollection<Sector> V5CurrentSectors { get; }
    Sector V5FocusedCurrentSector { get; set; }

    ImageEntry V5StoredImage { get; }
    DrawingImage V5StoredImageOverlay { get; }
    ObservableCollection<Sector> V5StoredSectors { get; }
    Sector V5FocusedStoredSector { get; set; }

    ObservableCollection<SectorDifferences> V5DiffSectors { get; }

    //L95xx
    ObservableCollection<Sector> L95xxCurrentSectors { get; }
    Sector L95xxFocusedCurrentSector { get; set; }

    ObservableCollection<Sector> L95xxStoredSectors { get; }
    Sector L95xxFocusedStoredSector { get; set; }

    ObservableCollection<SectorDifferences> L95xxDiffSectors { get; }
}