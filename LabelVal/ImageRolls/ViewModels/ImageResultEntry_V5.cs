using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.Messages;
using LabelVal.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using V5_REST_Lib.Models;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageResultEntry
{

    [ObservableProperty] private Databases.ImageResults.V5Result v5ResultRow;

    //public Config V5CurrentTemplate { get; set; }
    public JObject V5CurrentReport { get; private set; }
    public Results V5StoredReport { get; set; }

    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> v5CurrentSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.Sector> v5StoredSectors = [];
    [ObservableProperty] private ObservableCollection<Sectors.ViewModels.SectorDifferences> v5DiffSectors = [];

    [ObservableProperty] private byte[] v5Image = null;
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

        BringIntoView?.Invoke();

        if (SelectedScanner == null)
        {
            SendStatusMessage("No scanner selected.", SystemMessages.StatusMessageType.Error);
            return;
        }

        var res = await SelectedScanner.ScannerController.GetConfig();

        if (!res.OK)
        {
            SendErrorMessage("Could not get scanner configuration.");
            return;
        }

        var config = (JObject)res.Object;


        if (SelectedScanner.IsSimulator)
        {
            var fas = config["response"]["data"]["job"]["channelMap"]["acquisition"]["AcquisitionChannel"]["source"]["FileAcquisitionSource"];
            if (fas == null)
            {
                SendErrorMessage("The scanner is not in file aquire mode.");
                return;
            }

            //Rotate directory names to accomadate V5 
            var isFirst = fas["directory"].ToString() != SelectedScanner.FTPClient.ImagePath1Root;

            var path = isFirst
                ? SelectedScanner.FTPClient.ImagePath1
                : SelectedScanner.FTPClient.ImagePath2;

            fas["directory"] = isFirst
                ? SelectedScanner.FTPClient.ImagePath1Root
                : SelectedScanner.FTPClient.ImagePath2Root;

            SelectedScanner.FTPClient.Connect();

            if (!SelectedScanner.FTPClient.DirectoryExists(path))
                SelectedScanner.FTPClient.CreateRemoteDir(path);
            else
                SelectedScanner.FTPClient.DeleteRemoteFiles(path);

            path = $"{path}/image{System.IO.Path.GetExtension(SourceImagePath)}";

            if (imageType == "source")
                SelectedScanner.FTPClient.UploadFile(SourceImagePath, path);
            else if (imageType == "stored")
                SelectedScanner.FTPClient.UploadFile(V5Image, path);
            else if (imageType == "v275stored")
                SelectedScanner.FTPClient.UploadFile(V275Image, path);


            SelectedScanner.FTPClient.Disconnect();

            //Attempt to update the directory in the FileAcquisitionSource
            _ = await SelectedScanner.ScannerController.SendJob(config["response"]["data"]);


            _ = V5ProcessResults(await SelectedScanner.ScannerController.Trigger_Wait_Return(true));
        }
        else
            _ = V5ProcessResults(await SelectedScanner.ScannerController.Trigger_Wait_Return(true));


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

        if (!SelectedScanner.IsSimulator)
        {
            V5Image = ImageUtilities.ConvertToPng(triggerResults.FullImage);
            IsV5ImageStored = false;
        }
        else
        {
            if (V5Image == null)
            {
                V5Image = SourceImage.ToArray();
                IsV5ImageStored = false;
            }
        }

        V5CurrentSectors.Clear();

        List<Sectors.ViewModels.Sector> tempSectors = [];


        if (V5CurrentReport["event"]?["name"].ToString() == "cycle-report-alt")
        {
           foreach (var rSec in V5CurrentReport["event"]?["data"]?["decodeData"])
                tempSectors.Add(new Sectors.ViewModels.Sector(rSec.ToObject<Results_QualifiedResult>(), $"DecodeTool{rSec["toolSlot"]}", SelectedImageRoll.SelectedStandard, selectedImageRoll.SelectedGS1Table));

        }
        else if (V5CurrentReport["event"]?["name"].ToString() == "cycle-report")
        {
            foreach (var rSec in V5CurrentReport["event"]["data"]["cycleConfig"]["qualifiedResults"])
                tempSectors.Add(new Sectors.ViewModels.Sector(rSec.ToObject<Results_QualifiedResult>(), $"DecodeTool{rSec["toolSlot"]}", SelectedImageRoll.SelectedStandard, selectedImageRoll.SelectedGS1Table));
        }

        if (tempSectors.Count > 0)
        {
            tempSectors = tempSectors.OrderBy(x => x.Template.Top).ToList();

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
        V5ResultRow = SelectedDatabase.Select_V5Result(SelectedImageRoll.UID, SourceImageUID);

        if (V5ResultRow == null)
        {
            if (V5CurrentSectors.Count == 0)
            {
                V5Image = null;
                V5SectorsImageOverlay = null;
                IsV5ImageStored = false;
            }

            return;
        }

        var results = JsonConvert.DeserializeObject<JObject>(V5ResultRow.Report);

        V5Image = V5ResultRow.StoredImage;
        IsV5ImageStored = true;

        V5StoredSectors.Clear();

        List<Sectors.ViewModels.Sector> tempSectors = [];

        if (results["event"]?["name"].ToString() == "cycle-report-alt")
        {
            foreach (var rSec in results["event"]?["data"]?["decodeData"])
                tempSectors.Add(new Sectors.ViewModels.Sector(rSec.ToObject<Results_QualifiedResult>(), $"DecodeTool{rSec["toolSlot"]}", SelectedImageRoll.SelectedStandard, selectedImageRoll.SelectedGS1Table));

        }
        else if (results["event"]?["name"].ToString() == "cycle-report")
        {
            foreach (var rSec in results["event"]["data"]["cycleConfig"]["qualifiedResults"])
                tempSectors.Add(new Sectors.ViewModels.Sector(rSec.ToObject<Results_QualifiedResult>(), $"DecodeTool{rSec["toolSlot"]}", SelectedImageRoll.SelectedStandard, selectedImageRoll.SelectedGS1Table));
        }

        if (tempSectors.Count > 0)
        {
            tempSectors = tempSectors.OrderBy(x => x.Template.Top).ToList();

            foreach (var sec in tempSectors)
                V5StoredSectors.Add(sec);
        }

        V5SectorsImageOverlay = V5CreateSectorsImageOverlay(results);
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
        var bmp = ImageUtilities.CreateBitmap(V5Image);

        //Draw the image outline the same size as the stored image
        var border = new GeometryDrawing
        {
            Geometry = new RectangleGeometry(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)),
            Pen = new Pen(Brushes.Transparent, 1)
        };

        var secAreas = new GeometryGroup();
        var bndAreas = new GeometryGroup();

        var drwGroup = new DrawingGroup();


        if (results["event"]?["name"].ToString() == "cycle-report-alt")
        {
            foreach (var sec in results["event"]?["data"]?["decodeData"])
            {
                if (sec["boundingBox"] == null)
                    continue;

                secAreas.Children.Add(new RectangleGeometry(new Rect(new Point(sec["boundingBox"][0]["x"].Value<double>(), sec["boundingBox"][0]["y"].Value<double>()), new Point(sec["boundingBox"][2]["x"].Value<double>(), sec["boundingBox"][2]["y"].Value<double>()))));

                drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0, new Point(sec["boundingBox"][2]["x"].Value<double>() - 8, sec["boundingBox"][2]["y"].Value<double>() - 8))));
            }

            //foreach (var sec in results["event"]["data"]["cycleConfig"]["job"]["toolList"])
            //{
            //    foreach (var r in sec["SymbologyTool"]["regionList"])
            //        bndAreas.Children.Add(new RectangleGeometry(new Rect(r["Region"]["shape"]["RectShape"]["x"].Value<double>(), r["Region"]["shape"]["RectShape"]["y"].Value<double>(), r["Region"]["shape"]["RectShape"]["width"].Value<double>(), r["Region"]["shape"]["RectShape"]["height"].Value<double>())));
            //}
        }
        else if (results["event"]?["name"].ToString() == "cycle-report")
        {
            foreach (var sec in results["event"]["data"]["cycleConfig"]["qualifiedResults"])
            {
                if (sec["boundingBox"] == null)
                    continue;

                secAreas.Children.Add(new RectangleGeometry(new Rect(new Point(sec["boundingBox"][0]["x"].Value<double>(), sec["boundingBox"][0]["y"].Value<double>()), new Point(sec["boundingBox"][2]["x"].Value<double>(), sec["boundingBox"][2]["y"].Value<double>()))));

                drwGroup.Children.Add(new GlyphRunDrawing(Brushes.Black, CreateGlyphRun($"DecodeTool{sec["toolSlot"]}", new Typeface("Arial"), 30.0, new Point(sec["boundingBox"][2]["x"].Value<double>() - 8, sec["boundingBox"][2]["y"].Value<double>() - 8))));
            }

            foreach (var sec in results["event"]["data"]["cycleConfig"]["job"]["toolList"])
            {
                foreach (var r in sec["SymbologyTool"]["regionList"])
                    bndAreas.Children.Add(new RectangleGeometry(new Rect(r["Region"]["shape"]["RectShape"]["x"].Value<double>(), r["Region"]["shape"]["RectShape"]["y"].Value<double>(), r["Region"]["shape"]["RectShape"]["width"].Value<double>(), r["Region"]["shape"]["RectShape"]["height"].Value<double>())));
            }
        }


        var sectors = new GeometryDrawing
        {
            Geometry = secAreas,
            Pen = new Pen(Brushes.Red, 5)
        };

        var bounding = new GeometryDrawing
        {
            Geometry = bndAreas,
            Pen = new Pen(Brushes.Purple, 5)
        };

        drwGroup.Children.Add(bounding);
        drwGroup.Children.Add(sectors);
        drwGroup.Children.Add(border);

        var geometryImage = new DrawingImage(drwGroup);
        geometryImage.Freeze();
        return geometryImage;
    }

}
