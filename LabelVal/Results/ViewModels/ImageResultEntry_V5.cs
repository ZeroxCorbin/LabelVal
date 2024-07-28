using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.Databases;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using V5_REST_Lib.Models;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultEntry
{
    [ObservableProperty] private Databases.V5Result v5ResultRow;
    partial void OnV5ResultRowChanged(V5Result value) => V5StoredImage = value?.Stored;

    [ObservableProperty] private ImageEntry v5StoredImage;
    [ObservableProperty] private DrawingImage v5StoredImageOverlay;

    [ObservableProperty] private ImageEntry v5CurrentImage;
    [ObservableProperty] private DrawingImage v5CurrentImageOverlay;

    public V5_REST_Lib.Models.Config V5CurrentTemplate { get; set; }
    public V5_REST_Lib.Models.ResultsAlt V5CurrentReport { get; private set; }

    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISector> v5CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISector> v5StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.Interfaces.ISectorDifferences> v5DiffSectors = [];
    [ObservableProperty] private Sectors.Interfaces.ISector v5FocusedStoredSector = null;
    [ObservableProperty] private Sectors.Interfaces.ISector v5FocusedCurrentSector = null;

    [ObservableProperty] private bool isV5Working = false;
    partial void OnIsV5WorkingChanged(bool value) => OnPropertyChanged(nameof(IsNotV5Working));
    public bool IsNotV5Working => !IsV5Working;

    [ObservableProperty] private bool isV5Faulted = false;
    partial void OnIsV5FaultedChanged(bool value) => OnPropertyChanged(nameof(IsNotV5Faulted));
    public bool IsNotV5Faulted => !IsV5Faulted;

    [RelayCommand]
    private async Task V5Process(string imageType)
    {
        IsV5Faulted = false;
        IsV5Working = true;

        BringIntoView?.Invoke();

        if (ImageResults.SelectedScanner == null)
        {
            LogError("No scanner selected.");
            IsV5Working = false;
            return;
        }

        V5_REST_Lib.Commands.Results res = await ImageResults.SelectedScanner.ScannerController.GetConfig();

        if (!res.OK)
        {
            LogError("Could not get scanner configuration.");
            IsV5Working = false;
            return;
        }

        V5_REST_Lib.Models.Config config = (V5_REST_Lib.Models.Config)res.Object;

        if (imageType != "sensor")
        {
            V5_REST_Lib.Models.Config.Fileacquisitionsource fas = config.response.data.job.channelMap.acquisition.AcquisitionChannel.source.FileAcquisitionSource;
            if (fas == null)
            {
                LogError("The scanner is not in file aquire mode.");
                IsV5Working = false;
                return;
            }

            //Rotate directory names to accomadate V5 
            bool isFirst = fas.directory != ImageResults.SelectedScanner.FTPClient.ImagePath1Root;

            string path = isFirst
                ? ImageResults.SelectedScanner.FTPClient.ImagePath1
                : ImageResults.SelectedScanner.FTPClient.ImagePath2;

            fas.directory = isFirst
                ? ImageResults.SelectedScanner.FTPClient.ImagePath1Root
                : ImageResults.SelectedScanner.FTPClient.ImagePath2Root;

            ImageResults.SelectedScanner.FTPClient.Connect();

            if (!ImageResults.SelectedScanner.FTPClient.DirectoryExists(path))
                ImageResults.SelectedScanner.FTPClient.CreateRemoteDir(path);
            else
                ImageResults.SelectedScanner.FTPClient.DeleteRemoteFiles(path);

            path = $"{path}/image.png";

            if (imageType == "source")
                ImageResults.SelectedScanner.FTPClient.UploadFile(SourceImage.GetPngBytes(), path);
            else if (imageType == "v5Stored")
                ImageResults.SelectedScanner.FTPClient.UploadFile(V5ResultRow.Stored.GetPngBytes(), path);

            ImageResults.SelectedScanner.FTPClient.Disconnect();

            //Attempt to update the directory in the FileAcquisitionSource
            //config.response.data.job.channelMap.acquisition.AcquisitionChannel.source.uid = DateTime.Now.Ticks.ToString();
            _ = await ImageResults.SelectedScanner.ScannerController.SendJob(config.response.data);

            _ = V5ProcessResults(await ImageResults.SelectedScanner.ScannerController.Trigger_Wait_Return(true), config);
        }
        else
            _ = V5ProcessResults(await ImageResults.SelectedScanner.ScannerController.Trigger_Wait_Return(true), config);

        IsV5Working = false;
    }
    public bool V5ProcessResults(V5_REST_Lib.Controller.TriggerResults triggerResults, Config config)
    {
        if (!triggerResults.OK)
        {
            LogError("Could not trigger the scanner.");

            ClearRead("V5");

            return false;
        }

        V5CurrentImage = new ImageEntry(RollUID, triggerResults.FullImage, 600);
        V5CurrentTemplate = config;
        V5CurrentReport = JsonConvert.DeserializeObject<V5_REST_Lib.Models.ResultsAlt>(triggerResults.ReportJSON);

        V5CurrentSectors.Clear();

        List<Sectors.Interfaces.ISector> tempSectors = [];
        foreach (var rSec in V5CurrentReport._event.data.decodeData)
          tempSectors.Add(new V5.Sectors.Sector(rSec, V5CurrentTemplate.response.data.job.toolList[rSec.toolSlot-1], $"DecodeTool{rSec.toolSlot}", ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (Sectors.Interfaces.ISector sec in tempSectors)
                V5CurrentSectors.Add(sec);
        }

        V5GetSectorDiff();

        V5CurrentImageOverlay = CreateSectorsImageOverlay(V5CurrentImage, V5CurrentSectors);
       
        return true;
    }
    [RelayCommand] private void V5Load() => _ = V5LoadTask();

    private void V5GetStored()
    {
        if (SelectedDatabase == null)
            return;

        V5StoredSectors.Clear();

        V5ResultRow = SelectedDatabase.Select_V5Result(RollUID, ImageUID);

        if (V5ResultRow == null)
        {
            LogDebug("No V5 result found.");
            return;
        }

        if(V5ResultRow.Report == null || V5ResultRow.Template == null)
        {
            LogDebug("V5 result is missing data.");
            return;
        }

        List<Sectors.Interfaces.ISector> tempSectors = [];
        if (!string.IsNullOrEmpty(V5ResultRow.Report))
        {
            foreach (var rSec in V5ResultRow._Report._event.data.decodeData)
                tempSectors.Add(new V5.Sectors.Sector(rSec, V5ResultRow._Config.response.data.job.toolList[rSec.toolSlot-1], $"DecodeTool{rSec.toolSlot}", ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));
        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (Sectors.Interfaces.ISector sec in tempSectors)
                V5StoredSectors.Add(sec);
        } 
        
        V5StoredImageOverlay = CreateSectorsImageOverlay(V5StoredImage, V5StoredSectors);
    }
    private void V5GetSectorDiff()
    {
        V5DiffSectors.Clear();

        List<Sectors.Interfaces.ISectorDifferences> diff = [];

        //Compare; Do not check for missing here. To keep found at top of list.
        foreach (Sectors.Interfaces.ISector sec in V5StoredSectors)
        {
            foreach (Sectors.Interfaces.ISector cSec in V5CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Template.SymbologyType == cSec.Template.SymbologyType)
                    {
                        diff.Add(sec.SectorDifferences.Compare(cSec.SectorDifferences));
                        continue;
                    }
                    else
                    {
                        V5.Sectors.SectorDifferences dat = new()
                        {
                            UserName = $"{sec.Template.Username} (SYMBOLOGY MISMATCH)",
                            IsSectorMissing = true,
                            SectorMissingText = $"Stored Sector {sec.Template.SymbologyType} : Current Sector {cSec.Template.SymbologyType}"
                        };
                        diff.Add(dat);
                    }
                }
        }

        //Check for missing
        foreach (Sectors.Interfaces.ISector sec in V5StoredSectors)
        {
            bool found = false;
            foreach (Sectors.Interfaces.ISector cSec in V5CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    found = true;
                    continue;
                }

            if (!found)
            {
                V5.Sectors.SectorDifferences dat = new()
                {
                    UserName = $"{sec.Template.Username} (MISSING)",
                    IsSectorMissing = true,
                    SectorMissingText = "Not found in current Sectors"
                };
                diff.Add(dat);
            }
        }

        //check for missing
        if (V5StoredSectors.Count > 0)
            foreach (Sectors.Interfaces.ISector sec in V5CurrentSectors)
            {
                bool found = false;
                foreach (Sectors.Interfaces.ISector cSec in V5StoredSectors)
                    if (sec.Template.Name == cSec.Template.Name)
                    {
                        found = true;
                        continue;
                    }

                if (!found)
                {
                    V5.Sectors.SectorDifferences dat = new()
                    {
                        UserName = $"{sec.Template.Username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Stored Sectors"
                    };
                    diff.Add(dat);
                }
            }

        foreach (Sectors.Interfaces.ISectorDifferences d in diff)
            if (d.IsNotEmpty || d.IsSectorMissing)
                V5DiffSectors.Add(d);
    }
    public int V5LoadTask() => 1;

    private DrawingImage V5CreateSectorsImageOverlay(V5_REST_Lib.Models.ResultsAlt results, ImageRolls.ViewModels.ImageEntry image)
    {
        DrawingGroup drwGroup = new();

        //Draw the image outline the same size as the stored image
        GeometryDrawing border = new()
        {
            Geometry = new RectangleGeometry(new Rect(0.5, 0.5, image.Image.PixelWidth - 1, image.Image.PixelHeight - 1)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        GeometryGroup secCenter = new();
        GeometryGroup bndAreas = new();

        foreach (var sec in results._event.data.decodeData)
        {
            if (sec.boundingBox == null)
                continue;

            GeometryGroup secAreas = new();

            double brushWidth = 4.0;
            double halfBrushWidth = brushWidth / 2.0;

            for (int i = 0; i < 4; i++)
            {
                int nextIndex = (i + 1) % 4;

                double dx = sec.boundingBox[nextIndex].x - sec.boundingBox[i].x;
                double dy = sec.boundingBox[nextIndex].y - sec.boundingBox[i].y;

                // Calculate the length of the line segment
                double length = Math.Sqrt((dx * dx) + (dy * dy));

                // Normalize the direction to get a unit vector
                double ux = dx / length;
                double uy = dy / length;

                // Calculate the normal vector (perpendicular to the direction)
                double nx = -uy;
                double ny = ux;

                // Calculate the adjustment vector
                double ax = nx * halfBrushWidth;
                double ay = ny * halfBrushWidth;

                // Adjust the points
                double startX = sec.boundingBox[i].x - ax;
                double startY = sec.boundingBox[i].y - ay;
                double endX = sec.boundingBox[nextIndex].x - ax;
                double endY = sec.boundingBox[nextIndex].y - ay;

                // Add the line to the geometry group
                secAreas.Children.Add(new LineGeometry(new Point(startX, startY), new Point(endX, endY)));
            }

            if (sec.grading != null)
            {
                double grade = double.TryParse(sec.grading.grade, out double g) ? g : 4.0;

                drwGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(GetGradeBrush(V5GetLetter(grade)), 4), secAreas));
            }
            else
            {
                drwGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(Brushes.Black, 4), secAreas));
            }
            drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun($"DecodeTool{sec.toolSlot}", new Typeface("Arial"), 30.0, new Point(sec.boundingBox[2].x - 8, sec.boundingBox[2].y - 8))));

            secCenter.Children.Add(new LineGeometry(new Point(sec.x + 10, sec.y), new Point(sec.x + -10, sec.y)));
            secCenter.Children.Add(new LineGeometry(new Point(sec.x, sec.y + 10), new Point(sec.x, sec.y + -10)));

        }

        GeometryDrawing sectorCenters = new()
        {
            Geometry = secCenter,
            Pen = new Pen(Brushes.Red, 4)
        };
        GeometryDrawing bounding = new()
        {
            Geometry = bndAreas,
            Pen = new Pen(Brushes.Purple, 5)
        };

        drwGroup.Children.Add(bounding);
        drwGroup.Children.Add(sectorCenters);

        DrawingImage geometryImage = new(drwGroup);
        geometryImage.Freeze();
        return geometryImage;
    }

    private string V5GetLetter(double grade) => grade switch
    {
        double i when i == 4.0 => "A",
        double i when i is < 4.0 and >= 3.0 => "B",
        double i when i is < 3.0 and >= 2.0 => "C",
        double i when i is < 2.0 and >= 1.0 => "D",
        double i when i is < 1.0 and >= 0.0 => "F",
        _ => throw new System.NotImplementedException(),
    };

}
