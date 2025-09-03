using BarcodeVerification.lib.Common;
using BarcodeVerification.lib.Extensions;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.Databases;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.L95.Sectors;
using LabelVal.Results.Databases;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Interfaces;
using Lvs95xx.lib.Core.Controllers;
using MahApps.Metro.Controls.Dialogs;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace LabelVal.Results.ViewModels;
public partial class ImageResultDeviceEntry_L95
    : ImageResultDeviceEntryBase, IRecipient<PropertyChangedMessage<FullReport>>
{
    public override ImageResultEntryDevices Device { get; } = ImageResultEntryDevices.L95;

    public override LabelHandlers Handler => ImageResultsManager?.SelectedL95?.Controller != null && ImageResultsManager.SelectedL95.Controller.IsConnected && ImageResultsManager.SelectedL95.Controller.ProcessState == Watchers.lib.Process.Win32_ProcessWatcherProcessState.Running ? ImageResultsManager.SelectedL95.Controller.IsSimulator
            ? ImageResultsManager.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow?.TemplateString)
                    ? LabelHandlers.SimulatorRestore
                    : LabelHandlers.SimulatorDetect
                : LabelHandlers.SimulatorTrigger
            : ImageResultsManager.SelectedImageRoll.SectorType == ImageRollSectorTypes.Dynamic
                ? !string.IsNullOrEmpty(ResultRow?.TemplateString)
                    ? LabelHandlers.CameraRestore
                    : LabelHandlers.CameraDetect
                : LabelHandlers.CameraTrigger
        : LabelHandlers.Offline;

    public ImageResultDeviceEntry_L95(ImageResultEntry imageResultsEntry) : base(imageResultsEntry)
    {
        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<FullReport> message)
    {
        if (IsSelected || IsWorking)
            _ = Application.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(message.NewValue, false));
    }

    public override void GetStored()
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(GetStored);
            return;
        }

        if (ImageResultEntry.SelectedDatabase == null)
        {
            Logger.Error("No image results database selected.");
            return;
        }

        StoredSectors.Clear();

        try
        {
            var row = ImageResultEntry.SelectedDatabase.Select_Result(Device, ImageResultEntry.ImageRollUID, ImageResultEntry.SourceImageUID, ImageResultEntry.ImageRollUID);

            if (row == null)
            {
                ResultRow = null;
                return;
            }

            List<ISector> tempSectors = [];
            if (row.Report != null && row.Report.ContainsKey("AllReports"))
            {
                foreach (var rSec in row.Report.GetParameter<JArray>("AllReports"))
                    tempSectors.Add(new Sector(((JObject)rSec).GetParameter<JObject>("Template"), ((JObject)rSec).GetParameter<JObject>("Report"), [ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGradingStandard], ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table, ((JObject)rSec).GetParameter<string>("Template.Settings[SettingName:Version].SettingValue")));
            }


            if (tempSectors.Count > 0)
            {
                tempSectors = ImageResultEntry.SortList3(tempSectors);

                foreach (var sec in tempSectors)
                    StoredSectors.Add(sec);
            }

            ResultRow = row;
            RefreshStoredOverlay();

        }
        catch (System.Exception ex)
        {
            Logger.Error(ex);
            Logger.Error($"Error while loading stored results from: {ImageResultEntry.SelectedDatabase.File.Name}");
        }
    }

    [RelayCommand]
    public async Task StoreSingle()
    {
        if (CurrentSectors.Count == 0 || CurrentSelectedSector == null)
        {
            Logger.Error("No sector selected to store.");
            return;
        }

        if (ImageResultEntry.SelectedDatabase == null)
        {
            Logger.Error("No image results database selected.");
            return;
        }

        var old = StoredSectors.FirstOrDefault(x => x.Template.Name == CurrentSelectedSector.Template.Name);
        if (old != null)
        {
            if (await ImageResultEntry.OkCancelDialog("Overwrite Stored Sector", $"The sector already exists.\r\nAre you sure you want to overwrite the stored sector?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                return;
        }

        //Save the list to the database.
        List<FullReport> temp = [];
        foreach (var sector in StoredSectors.Where(s => s.Template.Name != CurrentSelectedSector.Template.Name))
            temp.Add(new FullReport(((Sector)sector).Template.Original, ((Sector)sector).Report.Original));

        temp.Add(new FullReport(((Sector)CurrentSelectedSector).Template.Original, ((Sector)CurrentSelectedSector).Report.Original));

        JObject report = new()
        {
            ["AllReports"] = JArray.FromObject(temp)
        };

        _ = ImageResultEntry.SelectedDatabase.InsertOrReplace_Result(new Databases.Result
        {
            Device = Device,
            ImageRollUID = ImageResultEntry.ImageRollUID,
            SourceImageUID = ImageResultEntry.SourceImageUID,
            RunUID = ImageResultEntry.ImageRollUID,

            Template = CurrentTemplate,
            Report = report,
            Stored = CurrentImage,
        });

        GetStored();
        _ = CurrentSectors.Remove(CurrentSelectedSector);
        GetSectorDiff();
    }
    public override async Task Store()
    {
        if (CurrentSectors.Count == 0)
        {
            Logger.Error("No sectors to store.");
            return;
        }

        if (ImageResultEntry.SelectedDatabase == null)
        {
            Logger.Error("No image results database selected.");
            return;
        }

        if (StoredSectors.Count > 0)
            if (await ImageResultEntry.OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                return;

        var res = GetCurrentReport();

        if (ImageResultEntry.SelectedDatabase.InsertOrReplace_Result(res) == null)
            Logger.Error($"Error while storing results to: {ImageResultEntry.SelectedDatabase.File.Name}");

        GetStored();
        ClearCurrent();
    }

    private Result GetCurrentReport()
    {
        //Save the list to the database.
        List<FullReport> temp = [];
        foreach (var sector in CurrentSectors)
            temp.Add(new FullReport(((Sector)sector).Template.Original, ((Sector)sector).Report.Original));

        JObject report = new()
        {
            ["AllReports"] = JArray.FromObject(temp)
        };

        var res = new Databases.Result
        {
            Device = Device,
            ImageRollUID = ImageResultEntry.ImageRollUID,
            SourceImageUID = ImageResultEntry.SourceImageUID,
            RunUID = ImageResultEntry.ImageRollUID,

            Template = CurrentTemplate,
            Report = report,
            Stored = CurrentImage,
        };

        return res;
    }

    public override void Process()
    {
        Label lab = new()
        {
            Config = new Config()
            {
                ApplicationStandard = ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard.GetDescription(),
            },
            RepeatAvailable = (report, replace) => ProcessFullReport(report, replace),
        };

        if (ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard == ApplicationStandards.GS1)
            lab.Config.Table = ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table.GetTableName();

        if (ImageResultEntry.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Source)
            lab.Image = ImageResultEntry.SourceImage.BitmapBytes;
        else if (ImageResultEntry.ImageResultsManager.SelectedImageRoll.ImageType == ImageRollImageTypes.Stored)
            lab.Image = ResultRow.Stored.BitmapBytes;

        IsWorking = true;
        IsFaulted = false;

        _ = Task.Run(() => ImageResultEntry.ImageResultsManager.SelectedL95.Controller.ProcessLabelAsync(lab));
    }
    public void ProcessFullReport(FullReport message, bool replaceSectors)
    {
        if (!Application.Current.Dispatcher.CheckAccess())
        {
            _ = Application.Current.Dispatcher.BeginInvoke(() => ProcessFullReport(message, replaceSectors));
            return;
        }

        try
        {

            if (message == null || message.Report == null)
            {
                IsFaulted = true;
                return;
            }

            System.Drawing.Point center = new(message.Template.GetParameter<int>("Report.X1") + (message.Template.GetParameter<int>("Report.SizeX") / 2), message.Template.GetParameter<int>("Report.Y1") + (message.Template.GetParameter<int>("Report.SizeY") / 2));

            string name = null;
            if ((name = ImageResultEntry.GetName(center)) == null)
                name ??= $"Verify_{CurrentSectors.Count + 1}";

            _ = message.Template.SetParameter("Name", name);

            if (replaceSectors)
                CurrentSectors.Clear();

            CurrentSectors.Add(new Sector(message.Template, message.Report, [ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGradingStandard], ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedApplicationStandard, ImageResultEntry.ImageResultsManager.SelectedImageRoll.SelectedGS1Table, message.Template.GetParameter<string>("Settings[SettingName:Version].SettingValue")));

            var tempSectors = CurrentSectors.ToList();

            if (tempSectors.Count > 0)
            {
                tempSectors = ImageResultEntry.SortList3(tempSectors);
                SortObservableCollectionByList(tempSectors, CurrentSectors);
            }

            GetSectorDiff();

            CurrentImage = new ImageEntry(ImageResultEntry.ImageRollUID, message.Template.GetParameter<byte[]>("Report.Thumbnail"), 0);
            RefreshCurrentOverlay();

            IsFaulted = false;
        }
        catch (System.Exception ex)
        {
            Logger.Error(ex);
            IsFaulted = true;
        }
        finally
        {
            IsWorking = false;
            Application.Current.Dispatcher.Invoke(ImageResultEntry.BringIntoViewHandler);
        }
    }

    [RelayCommand]
    public void ClearSingle()
    {
        if (CurrentSelectedSector == null)
        {
            Logger.Error("No sector selected to clear.");
            return;
        }

        _ = CurrentSectors.Remove(CurrentSelectedSector);

        if (CurrentSectors.Count == 0)
        {
            DiffSectors.Clear();
            CurrentImage = null;
            CurrentImageOverlay = null;
        }
        else
        {
            GetSectorDiff();
            CurrentImageOverlay = IImageResultDeviceEntry.CreateSectorsImageOverlay(CurrentImage, CurrentSectors);
        }
    }

    protected override void GetSectorDiff()
    {
        DiffSectors.Clear();

        List<SectorDifferences> diff = [];

        //Compare; Do not check for missing her. To keep found at top of list.
        foreach (Sector sec in StoredSectors)
        {
            foreach (Sector cSec in CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    if (sec.Report.Symbology == cSec.Report.Symbology)
                    {
                        var res = sec.SectorDetails.Compare(cSec.SectorDetails);
                        if (res == null)
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
                            SectorMissingText = $"Stored Sector {sec.Report.Symbology.GetDescription()} : Current Sector {cSec.Report.Symbology.GetDescription()}"
                        };
                        diff.Add(dat);
                    }
                }
        }

        //Check for missing
        foreach (Sector sec in StoredSectors)
        {
            var found = false;
            foreach (Sector cSec in CurrentSectors)
                if (sec.Template.Name == cSec.Template.Name)
                {
                    found = true;
                    break;
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
        if (StoredSectors.Count > 0)
            foreach (Sector sec in CurrentSectors)
            {
                var found = false;
                foreach (Sector cSec in StoredSectors)
                    if (sec.Template.Name == cSec.Template.Name)
                    {
                        found = true;
                        break;
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

        foreach (var d in diff)
            DiffSectors.Add(d);
    }

    public static void SortObservableCollectionByList(List<ISector> list, ObservableCollection<ISector> observableCollection)
    {
        for (var i = 0; i < list.Count; i++)
        {
            var item = list[i];
            var currentIndex = observableCollection.IndexOf(item);
            if (currentIndex != i && currentIndex != -1)
            {
                observableCollection.Move(currentIndex, i);
            }
        }
    }
}