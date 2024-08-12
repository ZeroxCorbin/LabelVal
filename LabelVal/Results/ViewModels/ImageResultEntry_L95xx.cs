using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.LVS_95xx.Sectors;
using LabelVal.Sectors.Interfaces;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using static V5_REST_Lib.Controller;
using V5_REST_Lib.Models;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultEntry : IRecipient<PropertyChangedMessage<LabelVal.LVS_95xx.Models.FullReport>>
{
    [ObservableProperty] private Databases.L95xxResult l95xxResultRow;
    partial void OnL95xxResultRowChanged(Databases.L95xxResult value) => L95xxStoredImage = L95xxResultRow?.Stored;

    [ObservableProperty] private ImageEntry l95xxStoredImage;
    [ObservableProperty] private DrawingImage l95xxStoredImageOverlay;

    [ObservableProperty] private ImageEntry l95xxCurrentImage;
    [ObservableProperty] private DrawingImage l95xxCurrentImageOverlay;

    public List<LVS_95xx.Models.FullReport> L95xxCurrentReport { get; private set; }

    public ObservableCollection<Sectors.Interfaces.ISector> L95xxCurrentSectors { get; } = [];
    public ObservableCollection<Sectors.Interfaces.ISector> L95xxStoredSectors { get; } = [];
    public ObservableCollection<Sectors.Interfaces.ISectorDifferences> L95xxDiffSectors { get; } = [];

    [ObservableProperty] private Sectors.Interfaces.ISector l95xxFocusedStoredSector = null;
    [ObservableProperty] private Sectors.Interfaces.ISector l95xxFocusedCurrentSector = null;

    [ObservableProperty] private Sectors.Interfaces.ISector l95xxCurrentSectorSelected;

    //[ObservableProperty] private byte[] l95xxImage = null;
    //[ObservableProperty] private DrawingImage l95xxSectorsImageOverlay;
    //[ObservableProperty] private bool isL95xxImageStored;

    [ObservableProperty] private bool isL95xxWorking = false;
    partial void OnIsL95xxWorkingChanged(bool value) => OnPropertyChanged(nameof(IsNotL95xxWorking));
    public bool IsNotL95xxWorking => !IsL95xxWorking;

    [ObservableProperty] private bool isL95xxFaulted = false;
    partial void OnIsL95xxFaultedChanged(bool value) => OnPropertyChanged(nameof(IsNotL95xxFaulted));
    public bool IsNotL95xxFaulted => !IsL95xxFaulted;

    [ObservableProperty] private bool isL95xxSelected = false;

    public void Receive(PropertyChangedMessage<LabelVal.LVS_95xx.Models.FullReport> message)
    {
        if (IsL95xxSelected)
            App.Current.Dispatcher.BeginInvoke(() => L95xxProcess(message.NewValue));

        
    }

    public static void SortObservableCollectionByList(List<ISector> list, ObservableCollection<ISector> observableCollection)
    {
        for (int i = 0; i < list.Count; i++)
        {
            ISector item = list[i];
            int currentIndex = observableCollection.IndexOf(item);
            if (currentIndex != i)
            {
                observableCollection.Move(currentIndex, i);
            }
        }
    }

    private void L95xxGetStored()
    {
        if (SelectedDatabase == null)
            return;

        L95xxStoredSectors.Clear();

        L95xxResultRow = SelectedDatabase.Select_L95xxResult(ImageRollUID, SourceImageUID);

        if (L95xxResultRow == null)
        {
            return;
        }

        List<LVS_95xx.Models.FullReport> report = L95xxResultRow._Report;

        L95xxStoredSectors.Clear();
        List<Sectors.Interfaces.ISector> tempSectors = [];
        foreach (var rSec in report)
            tempSectors.Add(new Sector(rSec, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));

        if (tempSectors.Count > 0)
        {
            SortList(tempSectors);

            foreach (Sector sec in tempSectors)
                L95xxStoredSectors.Add(sec);
        }

        UpdateL95xxStoredImageOverlay();
    }

    private void L95xxProcess(LabelVal.LVS_95xx.Models.FullReport message)
    {
        if(message == null || message.Report == null)
            return;

        if(message.Report.OverallGrade.StartsWith("Bar"))
            return;
        
        var center = new System.Drawing.Point(message.Report.X1 + (message.Report.SizeX / 2), message.Report.Y1 + (message.Report.SizeY / 2));

        string name = null;

        foreach (var sec in L95xxStoredSectors)
            if (ISector.FallsWithin(sec, center))
                name = sec.Template.Username;

        if (name == null)
            foreach (var sec in V275StoredSectors)
                if (ISector.FallsWithin(sec, center))
                    name = sec.Template.Username;

        if (name == null)
            foreach (var sec in V5StoredSectors)
                if (ISector.FallsWithin(sec, center))
                    name = sec.Template.Username;

        if (name == null)
            name = $"Verify_{L95xxCurrentSectors.Count + 1}";

        message.Name = name;

        L95xxCurrentSectors.Add(new Sector(message, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));
        List<ISector> secs = L95xxCurrentSectors.ToList();
        SortList(secs);
        SortObservableCollectionByList(secs, L95xxCurrentSectors);


        L95xxCurrentImage = new ImageEntry(ImageRollUID, message.Report.Thumbnail, 600);
        UpdateL95xxCurrentImageOverlay();
        //V5CurrentTemplate = config;
        //V5CurrentReport = JsonConvert.DeserializeObject<V5_REST_Lib.Models.ResultsAlt>(triggerResults.ReportJSON);

        //V5CurrentSectors.Clear();

        //List<Sectors.Interfaces.ISector> tempSectors = [];
        //foreach (ResultsAlt.Decodedata rSec in V5CurrentReport._event.data.decodeData)
        //    tempSectors.Add(new V5.Sectors.Sector(rSec, V5CurrentTemplate.response.data.job.toolList[rSec.toolSlot - 1], $"DecodeTool{rSec.toolSlot.ToString()}", ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));

        //if (tempSectors.Count > 0)
        //{
        //    SortList(tempSectors);

        //    foreach (Sectors.Interfaces.ISector sec in tempSectors)
        //        V5CurrentSectors.Add(sec);
        //}

        //V5GetSectorDiff();

        //V5CurrentImageOverlay = CreateSectorsImageOverlay(V5CurrentImage, V5CurrentSectors);

        //return true;
    }
    public void UpdateL95xxStoredImageOverlay() => L95xxStoredImageOverlay = CreateSectorsImageOverlay(L95xxStoredImage, L95xxStoredSectors);
    public void UpdateL95xxCurrentImageOverlay() => L95xxStoredImageOverlay = CreateSectorsImageOverlay(L95xxCurrentImage, L95xxCurrentSectors);

    private void L95xxGetSectorDiff()
    {
        L95xxDiffSectors.Clear();

        List<Sectors.Interfaces.ISectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (Sector sec in L95xxStoredSectors)
        {
            foreach (Sector cSec in L95xxCurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Template.SymbologyType == cSec.Template.SymbologyType)
                    {
                        diff.Add(sec.SectorDifferences.Compare(cSec.SectorDifferences));
                        continue;
                    }
                    else
                    {
                        LVS_95xx.Sectors.SectorDifferences dat = new()
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
        foreach (Sector sec in L95xxStoredSectors)
        {
            bool found = false;
            foreach (Sector cSec in L95xxCurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    found = true;
                    continue;
                }

            if (!found)
            {
                LVS_95xx.Sectors.SectorDifferences dat = new()
                {
                    UserName = $"{sec.Template.Username} (MISSING)",
                    IsSectorMissing = true,
                    SectorMissingText = "Not found in current Sectors"
                };
                diff.Add(dat);
            }
        }

        //check for missing
        if (L95xxStoredSectors.Count > 0)
            foreach (Sector sec in L95xxCurrentSectors)
            {
                bool found = false;
                foreach (Sector cSec in L95xxStoredSectors)
                    if (sec.Template.Name == cSec.Template.Name)
                    {
                        found = true;
                        continue;
                    }

                if (!found)
                {
                    LVS_95xx.Sectors.SectorDifferences dat = new()
                    {
                        UserName = $"{sec.Template.Username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Stored Sectors"
                    };
                    diff.Add(dat);
                }
            }

        foreach (LVS_95xx.Sectors.SectorDifferences d in diff)
            if (d.IsNotEmpty)
                L95xxDiffSectors.Add(d);
    }
    //public int L95xxLoadTask()
    //{
    //    return 1;
    //}
    //private DrawingImage L95xxCreateSectorsImageOverlay(bool useStored)
    //{
    //    var bmp = ImageUtilities.CreateBitmap(L95xxImage);

    //    //Draw the image outline the same size as the stored image
    //    var border = new GeometryDrawing
    //    {
    //        Geometry = new RectangleGeometry(new Rect(0, 0, bmp.PixelWidth, bmp.PixelHeight)),
    //        Pen = new Pen(Brushes.Transparent, 1)
    //    };

    //    var secAreas = new GeometryGroup();
    //    var bndAreas = new GeometryGroup();

    //    var drwGroup = new DrawingGroup();

    //    if (useStored)
    //    {
    //        foreach (var sec in L95xxStoredReport._event.data.cycleConfig.qualifiedResults)
    //        {
    //            if (sec.boundingBox == null)
    //                continue;

    //            secAreas.Children.Add(new RectangleGeometry(new Rect(new Point(sec.boundingBox[0].x, sec.boundingBox[0].y), new Point(sec.boundingBox[2].x, sec.boundingBox[2].y))));
    //        }

    //        foreach (var sec in L95xxStoredReport._event.data.cycleConfig.job.toolList)
    //            foreach (var r in sec.SymbologyTool.regionList)
    //                bndAreas.Children.Add(new RectangleGeometry(new Rect(r.Region.shape.RectShape.x, r.Region.shape.RectShape.y, r.Region.shape.RectShape.width, r.Region.shape.RectShape.height)));

    //    }
    //    else
    //    {
    //        foreach (var sec in L95xxCurrentReport._event.data.cycleConfig.qualifiedResults)
    //        {
    //            if (sec.boundingBox == null)
    //                continue;

    //            secAreas.Children.Add(new RectangleGeometry(new Rect(new Point(sec.boundingBox[0].x, sec.boundingBox[0].y), new Point(sec.boundingBox[2].x, sec.boundingBox[2].y))));
    //        }

    //        foreach (var sec in L95xxCurrentReport._event.data.cycleConfig.job.toolList)
    //            foreach (var r in sec.SymbologyTool.regionList)
    //                bndAreas.Children.Add(new RectangleGeometry(new Rect(r.Region.shape.RectShape.x, r.Region.shape.RectShape.y, r.Region.shape.RectShape.width, r.Region.shape.RectShape.height)));

    //    }

    //    var sectors = new GeometryDrawing
    //    {
    //        Geometry = secAreas,
    //        Pen = new Pen(Brushes.Red, 5)
    //    };

    //    var bounding = new GeometryDrawing
    //    {
    //        Geometry = bndAreas,
    //        Pen = new Pen(Brushes.Purple, 5)
    //    };

    //    drwGroup.Children.Add(bounding);
    //    drwGroup.Children.Add(sectors);
    //    drwGroup.Children.Add(border);

    //    var geometryImage = new DrawingImage(drwGroup);
    //    geometryImage.Freeze();
    //    return geometryImage;
    //}
}
