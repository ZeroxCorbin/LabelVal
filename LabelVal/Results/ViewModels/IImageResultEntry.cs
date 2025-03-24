using LabelVal.ImageRolls.ViewModels;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using System.Collections.ObjectModel;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;

//bool simAddSec = ImageResults.SelectedScanner.Controller.IsSimulator && ImageResults.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic && !string.IsNullOrEmpty(V5ResultRow?.Template);
//bool simDetSec = ImageResults.SelectedScanner.Controller.IsSimulator && ImageResults.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic && string.IsNullOrEmpty(V5ResultRow?.Template);
//bool camAddSec = !ImageResults.SelectedScanner.Controller.IsSimulator && ImageResults.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic && !string.IsNullOrEmpty(V5ResultRow?.Template);
//bool camDetSec = !ImageResults.SelectedScanner.Controller.IsSimulator && ImageResults.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic && string.IsNullOrEmpty(V5ResultRow?.Template);



public interface IImageResultEntry
{
    static string Version { get; set; } = "1.0";

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

    ObservableCollection<SectorDifferences> V275DiffSectors { get; }

    //V5
    ImageEntry V5CurrentImage { get; }
    DrawingImage V5CurrentImageOverlay { get; }
    ObservableCollection<ISector> V5CurrentSectors { get; }
    ISector V5FocusedCurrentSector { get; set; }

    ImageEntry V5StoredImage { get; }
    DrawingImage V5StoredImageOverlay { get; }
    ObservableCollection<ISector> V5StoredSectors { get; }
    ISector V5FocusedStoredSector { get; set; }

    ObservableCollection<SectorDifferences> V5DiffSectors { get; }

    //L95xx
    ObservableCollection<ISector> L95xxCurrentSectors { get; }
    ISector L95xxFocusedCurrentSector { get; set; }

    ObservableCollection<ISector> L95xxStoredSectors { get; }
    ISector L95xxFocusedStoredSector { get; set; }

    ObservableCollection<SectorDifferences> L95xxDiffSectors { get; }
}