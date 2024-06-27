using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Messages;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultEntry
{

    [ObservableProperty] private Databases.ImageResults.V5Result v5ResultRow;

    //public Config V5CurrentTemplate { get; set; }
    public JObject V5CurrentReport { get; private set; }
    public V5_REST_Lib.Models.Results V5StoredReport { get; set; }

    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> v5CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> v5StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.SectorDifferences> v5DiffSectors = [];
    [ObservableProperty] private Sectors.ViewModels.Sector v5FocusedStoredSector = null;
    [ObservableProperty] private Sectors.ViewModels.Sector v5FocusedCurrentSector = null;

    [ObservableProperty] private ImageEntry v5Image = null;
    [ObservableProperty] private DrawingImage v5SectorsImageOverlay;
    [ObservableProperty] private bool isV5ImageStored;

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
            SendStatusMessage("No scanner selected.", SystemMessages.StatusMessageType.Error);
            return;
        }

        var res = await ImageResults.SelectedScanner.ScannerController.GetConfig();

        if (!res.OK)
        {
            SendErrorMessage("Could not get scanner configuration.");
            return;
        }

        var config = (V5_REST_Lib.Models.Config)res.Object;


        if (ImageResults.SelectedScanner.IsSimulator)
        {
            var fas = config.response.data.job.channelMap.acquisition.AcquisitionChannel.source.FileAcquisitionSource;
            //var fas = config["response"]["data"]["job"]["channelMap"]["acquisition"]["AcquisitionChannel"]["source"]["FileAcquisitionSource"];
            if (fas == null)
            {
                SendErrorMessage("The scanner is not in file aquire mode.");
                return;
            }

            //Rotate directory names to accomadate V5 
            var isFirst = fas.directory != ImageResults.SelectedScanner.FTPClient.ImagePath1Root;

            var path = isFirst
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
            else if (imageType == "v275Stored")
                ImageResults.SelectedScanner.FTPClient.UploadFile(V275ResultRow.Stored.GetPngBytes(), path);


            ImageResults.SelectedScanner.FTPClient.Disconnect();

            //Attempt to update the directory in the FileAcquisitionSource
            _ = await ImageResults.SelectedScanner.ScannerController.SendJob(config.response.data);


            _ = V5ProcessResults(await ImageResults.SelectedScanner.ScannerController.Trigger_Wait_Return(true));
        }
        else
            _ = V5ProcessResults(await ImageResults.SelectedScanner.ScannerController.Trigger_Wait_Return(true));


        IsV5Working = false;
    }

    public bool V5ProcessResults(V5_REST_Lib.Controller.TriggerResults triggerResults)
    {
        if (!triggerResults.OK)
        {
            SendErrorMessage("Could not trigger the scanner.");

            V5CurrentReport = null;

            if (!IsV5ImageStored)
            {
                V5Image = null;
                V5SectorsImageOverlay = null;
            }

            return false;
        }

        V5CurrentReport = JsonConvert.DeserializeObject<JObject>(triggerResults.ReportJSON);

        if (!ImageResults.SelectedScanner.IsSimulator)
        {
            V5Image = new ImageEntry(ImageResults.SelectedImageRoll.UID, triggerResults.FullImage, 600);
            //ImageUtilities.ConvertToPng(triggerResults.FullImage);
            IsV5ImageStored = false;
        }
        else
        {
            V5Image = SourceImage.Clone();
            IsV5ImageStored = false;
        }

        V5CurrentSectors.Clear();

        List<Sectors.ViewModels.Sector> tempSectors = [];


        if (V5CurrentReport["event"]?["name"].ToString() == "cycle-report-alt")
        {
            foreach (var rSec in V5CurrentReport["event"]?["data"]?["decodeData"])
                tempSectors.Add(new Sectors.ViewModels.Sector(rSec.ToObject<V5_REST_Lib.Models.Results_QualifiedResult>(), $"DecodeTool{rSec["toolSlot"]}", ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));

        }
        else if (V5CurrentReport["event"]?["name"].ToString() == "cycle-report")
        {
            foreach (var rSec in V5CurrentReport["event"]["data"]["cycleConfig"]["qualifiedResults"])
                tempSectors.Add(new Sectors.ViewModels.Sector(rSec.ToObject<V5_REST_Lib.Models.Results_QualifiedResult>(), $"DecodeTool{rSec["toolSlot"]}", ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));
        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (var sec in tempSectors)
                V5CurrentSectors.Add(sec);
        }

        V5GetSectorDiff();

        V5SectorsImageOverlay = V5CreateSectorsImageOverlay(V5CurrentReport);

        return true;
    }
    //[RelayCommand] private void V5Read() => _ = V5ReadTask();
    [RelayCommand] private void V5Load() => _ = V5LoadTask();

    //[RelayCommand] private void V5Inspect() => _ = V5ReadTask(0);

    
    private void V5GetStored()
    {
        V5ResultRow = SelectedDatabase.Select_V5Result(ImageResults.SelectedImageRoll.UID, SourceImage.UID);

        if (V5ResultRow == null)
        {
            V5StoredSectors.Clear();

            if (V5CurrentSectors.Count == 0)
            {
                V5Image = null;
                V5SectorsImageOverlay = null;
                IsV5ImageStored = false;
            }

            return;
        }

        V5Image = JsonConvert.DeserializeObject<ImageEntry>(V5ResultRow.StoredImage);
        IsV5ImageStored = true;

        V5StoredSectors.Clear();

        List<Sectors.ViewModels.Sector> tempSectors = [];
        if (!string.IsNullOrEmpty(V5ResultRow.Report))
        {
            var results = V5ResultRow._Report;

            V5SectorsImageOverlay = V5CreateSectorsImageOverlay(results);

            if (results["event"]?["name"].ToString() == "cycle-report-alt")
            {
                foreach (var rSec in results["event"]?["data"]?["decodeData"])
                    tempSectors.Add(new Sectors.ViewModels.Sector(rSec.ToObject<V5_REST_Lib.Models.Results_QualifiedResult>(), $"DecodeTool{rSec["toolSlot"]}", ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));

            }
            else if (results["event"]?["name"].ToString() == "cycle-report")
            {
                foreach (var rSec in results["event"]["data"]["cycleConfig"]["qualifiedResults"])
                    tempSectors.Add(new Sectors.ViewModels.Sector(rSec.ToObject<V5_REST_Lib.Models.Results_QualifiedResult>(), $"DecodeTool{rSec["toolSlot"]}", ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));
            }
        }

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (var sec in tempSectors)
                V5StoredSectors.Add(sec);
        }
    }

    private void V5GetSectorDiff()
    {
        V5DiffSectors.Clear();

        List<Sectors.ViewModels.SectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (var sec in V5StoredSectors)
        {
            foreach (var cSec in V5CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Template.Symbology == cSec.Template.Symbology)
                    {
                        diff.Add(sec.SectorDifferences.Compare(cSec.SectorDifferences));
                        continue;
                    }
                    else
                    {
                        var dat = new Sectors.ViewModels.SectorDifferences
                        {
                            UserName = $"{sec.Template.Username} (SYMBOLOGY MISMATCH)",
                            IsSectorMissing = true,
                            SectorMissingText = $"Stored Sector {sec.Template.Symbology} : Current Sector {cSec.Template.Symbology}"
                        };
                        diff.Add(dat);
                    }
                }
        }

        //Check for missing
        foreach (var sec in V5StoredSectors)
        {
            var found = false;
            foreach (var cSec in V5CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    found = true;
                    continue;
                }

            if (!found)
            {
                var dat = new Sectors.ViewModels.SectorDifferences
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
            foreach (var sec in V5CurrentSectors)
            {
                var found = false;
                foreach (var cSec in V5StoredSectors)
                    if (sec.Template.Name == cSec.Template.Name)
                    {
                        found = true;
                        continue;
                    }

                if (!found)
                {
                    var dat = new Sectors.ViewModels.SectorDifferences
                    {
                        UserName = $"{sec.Template.Username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Stored Sectors"
                    };
                    diff.Add(dat);
                }
            }

        foreach (var d in diff)
            if (d.IsNotEmpty)
                V5DiffSectors.Add(d);
    }
    public int V5LoadTask()
    {
        return 1;
    }

    
    private DrawingImage V5CreateSectorsImageOverlay(JObject results)
    {
        var drwGroup = new DrawingGroup();

        //Draw the image outline the same size as the stored image
        var border = new GeometryDrawing
        {
            Geometry = new RectangleGeometry(new Rect(0.5, 0.5, V5Image.Image.PixelWidth - 1, V5Image.Image.PixelHeight - 1)),
            Pen = new Pen(Brushes.Transparent, 1)
        };
        drwGroup.Children.Add(border);

        var secCenter = new GeometryGroup();
        var bndAreas = new GeometryGroup();

        if (results["event"]?["name"].ToString() == "cycle-report-alt")
        {
            foreach (var sec in results["event"]?["data"]?["decodeData"])
            {
                if (sec["boundingBox"] == null)
                    continue;

                var secAreas = new GeometryGroup();

                double brushWidth = 4.0;
                double halfBrushWidth = brushWidth / 2.0;

                for (int i = 0; i < 4; i++)
                {
                    int nextIndex = (i + 1) % 4;

                    double dx = sec["boundingBox"][nextIndex]["x"].Value<double>() - sec["boundingBox"][i]["x"].Value<double>();
                    double dy = sec["boundingBox"][nextIndex]["y"].Value<double>() - sec["boundingBox"][i]["y"].Value<double>();

                    // Calculate the length of the line segment
                    double length = Math.Sqrt(dx * dx + dy * dy);

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
                    double startX = sec["boundingBox"][i]["x"].Value<double>() - ax;
                    double startY = sec["boundingBox"][i]["y"].Value<double>() - ay;
                    double endX = sec["boundingBox"][nextIndex]["x"].Value<double>() - ax;
                    double endY = sec["boundingBox"][nextIndex]["y"].Value<double>() - ay;

                    // Add the line to the geometry group
                    secAreas.Children.Add(new LineGeometry(new Point(startX, startY), new Point(endX, endY)));
                }

                var grade = double.TryParse(sec["grading"]?["grade"].ToString(), out double g) ? g : 4.0;

                drwGroup.Children.Add(new GeometryDrawing(Brushes.Transparent, new Pen(GetGradeBrush(V5GetLetter(grade)), 4), secAreas));

                drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0, new Point(sec["boundingBox"][2]["x"].Value<double>() - 8, sec["boundingBox"][2]["y"].Value<double>() - 8))));

                secCenter.Children.Add(new LineGeometry(new Point(sec["x"].Value<double>() + 10, sec["y"].Value<double>()), new Point(sec["x"].Value<double>() + -10, sec["y"].Value<double>())));
                secCenter.Children.Add(new LineGeometry(new Point(sec["x"].Value<double>(), sec["y"].Value<double>() + 10), new Point(sec["x"].Value<double>(), sec["y"].Value<double>() + -10)));

             }
        }
        else if (results["event"]?["name"].ToString() == "cycle-report")
        {
            foreach (var sec in results["event"]["data"]["cycleConfig"]["qualifiedResults"])
            {
                if (sec["boundingBox"] == null)
                    continue;
                var secAreas = new GeometryGroup();
                secAreas.Children.Add(new LineGeometry(
                    new Point(sec["boundingBox"][0]["x"].Value<double>() - 2, sec["boundingBox"][0]["y"].Value<double>() - 2),
                    new Point(sec["boundingBox"][1]["x"].Value<double>() + 2, sec["boundingBox"][1]["y"].Value<double>() - 2)));
                secAreas.Children.Add(new LineGeometry(
                    new Point(sec["boundingBox"][1]["x"].Value<double>() + 2, sec["boundingBox"][1]["y"].Value<double>() - 2),
                    new Point(sec["boundingBox"][2]["x"].Value<double>() + 2, sec["boundingBox"][2]["y"].Value<double>() + 2)));
                secAreas.Children.Add(new LineGeometry(
                    new Point(sec["boundingBox"][2]["x"].Value<double>() + 2, sec["boundingBox"][2]["y"].Value<double>() + 2),
                    new Point(sec["boundingBox"][3]["x"].Value<double>() - 2, sec["boundingBox"][3]["y"].Value<double>() + 2)));
                secAreas.Children.Add(new LineGeometry(
                    new Point(sec["boundingBox"][3]["x"].Value<double>() - 2, sec["boundingBox"][3]["y"].Value<double>() + 2),
                    new Point(sec["boundingBox"][0]["x"].Value<double>() - 2, sec["boundingBox"][0]["y"].Value<double>() - 2)));

                drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0, new Point(sec["boundingBox"][2]["x"].Value<double>() - 8, sec["boundingBox"][2]["y"].Value<double>() - 8))));
            }

            foreach (var sec in results["event"]["data"]["cycleConfig"]["job"]["toolList"])
            {
                foreach (var r in sec["SymbologyTool"]["regionList"])
                    bndAreas.Children.Add(new RectangleGeometry(new Rect(r["Region"]["shape"]["RectShape"]["x"].Value<double>(), r["Region"]["shape"]["RectShape"]["y"].Value<double>(), r["Region"]["shape"]["RectShape"]["width"].Value<double>(), r["Region"]["shape"]["RectShape"]["height"].Value<double>())));
            }
        }

        var sectorCenters = new GeometryDrawing
        {
            Geometry = secCenter,
            Pen = new Pen(Brushes.Red, 4)
        };
        var bounding = new GeometryDrawing
        {
            Geometry = bndAreas,
            Pen = new Pen(Brushes.Purple, 5)
        };

        drwGroup.Children.Add(bounding);
        drwGroup.Children.Add(sectorCenters);
        
        var geometryImage = new DrawingImage(drwGroup);
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
