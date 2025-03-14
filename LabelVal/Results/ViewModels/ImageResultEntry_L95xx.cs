using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.LVS_95xx.Sectors;
using LabelVal.Sectors.Interfaces;
using LabelVal.Utilities;
using LibImageUtilities.ImageTypes.Png;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media;
using CommunityToolkit.Mvvm.Input;
using Lvs95xx.lib.Core.Controllers;
using System.Threading.Tasks;
using LabelVal.Sectors.Classes;
using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultEntry : IRecipient<PropertyChangedMessage<FullReport>>
{
    [ObservableProperty] private Databases.L95xxResult l95xxResultRow;
    partial void OnL95xxResultRowChanged(Databases.L95xxResult value) => L95xxStoredImage = L95xxResultRow?.Stored;

    [ObservableProperty] private ImageEntry l95xxStoredImage;
    [ObservableProperty] private DrawingImage l95xxStoredImageOverlay;

    [ObservableProperty] private ImageEntry l95xxCurrentImage;
    [ObservableProperty] private DrawingImage l95xxCurrentImageOverlay;

    public List<FullReport> L95xxCurrentReport { get; private set; }
    public string L95xxSerializeReport => JsonConvert.SerializeObject(L95xxCurrentReport);

    public ObservableCollection<Sectors.Interfaces.ISector> L95xxCurrentSectors { get; } = [];
    public ObservableCollection<Sectors.Interfaces.ISector> L95xxStoredSectors { get; } = [];
    public ObservableCollection<SectorDifferences> L95xxDiffSectors { get; } = [];

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
    partial void OnIsL95xxSelectedChanging(bool value) => ImageResults.IsL95xxSelected = ImageResults.IsL95xxSelected ? false : ImageResults.ResetL95xxSelected();

    public void Receive(PropertyChangedMessage<FullReport> message)
    {
        if (IsL95xxSelected || IsL95xxWorking)
            App.Current.Dispatcher.BeginInvoke(() => L95xxProcessResults(message.NewValue, false));
    }

    [RelayCommand]
    private void L95xxProcess(ImageResultEntryImageTypes imageType)
    {
        var lab = new Lvs95xx.lib.Core.Controllers.Label
        {
            Config = new Lvs95xx.lib.Core.Controllers.Config()
            {
                ApplicationStandard = ImageResults.SelectedImageRoll.SelectedStandard.GetDescription(),
            },
            RepeatAvailable = L95xxProcessResults,
        };

        if (ImageResults.SelectedImageRoll.SelectedStandard == AvailableStandards.GS1)
            lab.Config.Table = ImageResults.SelectedImageRoll.SelectedGS1Table.GetTableName();

        if (imageType == ImageResultEntryImageTypes.Source)
            lab.Image = SourceImage.BitmapBytes;
        else if (imageType == ImageResultEntryImageTypes.L95xxStored)
            lab.Image = L95xxResultRow.Stored.BitmapBytes;

        IsL95xxWorking = true;
        IsL95xxFaulted = false;

        Task.Run(() => ImageResults.SelectedVerifier.Controller.ProcessLabelAsync(lab));
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
        if (!App.Current.Dispatcher.CheckAccess())
        {
            _ = App.Current.Dispatcher.BeginInvoke(() => V275GetStored());
            return;
        }

        if (SelectedDatabase == null)
        {
            Logger.LogError("No image results database selected.");
            return;
        }

        L95xxStoredSectors.Clear();

        try
        {
            var row = SelectedDatabase.Select_L95xxResult(ImageRollUID, SourceImageUID);

            if (row == null)
            {
                L95xxResultRow = null;
                return;
            }
        
            List<Sectors.Interfaces.ISector> tempSectors = [];
            foreach (var rSec in row._Report)
                tempSectors.Add(new Sector(rSec, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));

            if (tempSectors.Count > 0)
            {
                SortList(tempSectors);

                foreach (var sec in tempSectors)
                    L95xxStoredSectors.Add(sec);
            }

            L95xxResultRow = row;
            UpdateL95xxStoredImageOverlay();

        }
        catch (Exception ex)
        {
            Logger.LogError(ex);
            Logger.LogError($"Error while loading stored results from: {SelectedDatabase.File.Name}");
        }
    }

    public void L95xxProcessResults(FullReport? message, bool replaceSectors)
    {
        if(!App.Current.Dispatcher.CheckAccess())
        {
            App.Current.Dispatcher.BeginInvoke(() => L95xxProcessResults(message, replaceSectors));
            return;
        }

        if (message == null || message.Report == null || message.Report.OverallGrade.StartsWith("Bar"))
        {
            IsL95xxFaulted = true;
            IsL95xxWorking = false;
            return;
        }

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

        if(replaceSectors)
            L95xxCurrentSectors.Clear();

        L95xxCurrentSectors.Add(new Sector(message, ImageResults.SelectedImageRoll.SelectedStandard, ImageResults.SelectedImageRoll.SelectedGS1Table));
        List<ISector> secs = L95xxCurrentSectors.ToList();
        SortList(secs);
        SortObservableCollectionByList(secs, L95xxCurrentSectors);

        L95xxGetSectorDiff();

        L95xxCurrentImage = new ImageEntry(ImageRollUID, LibImageUtilities.ImageTypes.Png.Utilities.GetPng(message.Image), 600);
        UpdateL95xxCurrentImageOverlay();  
        
        IsL95xxWorking = false;
    }
    public void UpdateL95xxStoredImageOverlay() => L95xxStoredImageOverlay = CreateSectorsImageOverlay(L95xxStoredImage, L95xxStoredSectors);
    public void UpdateL95xxCurrentImageOverlay() => L95xxCurrentImageOverlay = CreateSectorsImageOverlay(L95xxCurrentImage, L95xxCurrentSectors);

    private void L95xxGetSectorDiff()
    {
        L95xxDiffSectors.Clear();

        List<SectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (Sector sec in L95xxStoredSectors)
        {
            foreach (Sector cSec in L95xxCurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Template.SymbologyType == cSec.Template.SymbologyType)
                    {
                        var res = sec.SectorDetails.Compare(cSec.SectorDetails);
                        if(res == null)
                            continue;
                        diff.Add(res);
                        continue;
                    }
                    else
                    {
                        SectorDifferences dat = new()
                        {
                            Username = $"{sec.Template.Username} (SYMBOLOGY MISMATCH)",
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
                SectorDifferences dat = new()
                {
                    Username = $"{sec.Template.Username} (MISSING)",
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
                    SectorDifferences dat = new()
                    {
                        Username = $"{sec.Template.Username} (MISSING)",
                        IsSectorMissing = true,
                        SectorMissingText = "Not found in Stored Sectors"
                    };
                    diff.Add(dat);
                }
            }

        foreach (SectorDifferences d in diff)

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
