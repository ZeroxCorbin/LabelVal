using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using LabelVal.Utilities;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using V275_REST_lib.Models;
using V5_REST_Lib.Models;

namespace LabelVal.ImageRolls.ViewModels;

public partial class ImageResultEntry : ObservableRecipient,
    IRecipient<NodeMessages.SelectedNodeChanged>,
    IRecipient<DatabaseMessages.SelectedDatabseChanged>,
    IRecipient<ScannerMessages.SelectedScannerChanged>
{

    public delegate void BringIntoViewDelegate();
    public event BringIntoViewDelegate BringIntoView;

    //public delegate void StatusChange(string status);
    //public event StatusChange StatusChanged;

    //[ObservableProperty] private string status;
    //partial void OnStatusChanged(string value) => App.Current.Dispatcher.Invoke(() => StatusChanged?.Invoke(Status));

    public string SourceImagePath { get; }
    [ObservableProperty] private byte[] sourceImage;
    [ObservableProperty] private string sourceImageUID;
    [ObservableProperty] private string sourceImageComment;


    //[ObservableProperty] private bool v275SectorsNeedStored = false;
    //partial void OnV275SectorsNeedStoredChanged(bool value) => OnPropertyChanged(nameof(NotV275SectorsNeedStored));
    //public bool NotV275SectorsNeedStored => !V275SectorsNeedStored;

    [ObservableProperty] private Node selectedNode;
    [ObservableProperty] private ImageRollEntry selectedImageRoll;
    [ObservableProperty] private Scanner selectedScanner;
    [ObservableProperty] private Databases.ImageResults selectedDatabase;
    partial void OnSelectedDatabaseChanged(Databases.ImageResults value) => GetStored();

    [ObservableProperty] private Sectors.ViewModels.Sector selectedSector;

    private static IDialogCoordinator DialogCoordinator => MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance;
    public ImageResultEntry(string imagePath, string imageComment, Node selectedNode, ImageRollEntry selectedImageRoll, Databases.ImageResults selectedDatabase, Scanner selectedScanner)
    {
        SourceImagePath = imagePath;
        SourceImageComment = imageComment;

        GetImage(imagePath);

        SelectedImageRoll = selectedImageRoll;
        SelectedNode = selectedNode;
        SelectedDatabase = selectedDatabase;
        SelectedScanner = selectedScanner;

        IsActive = true;
    }

    private void SendStatusMessage(string message, SystemMessages.StatusMessageType type) => WeakReferenceMessenger.Default.Send(new SystemMessages.StatusMessage(this, type, message));
    private void SendErrorMessage(string message) => WeakReferenceMessenger.Default.Send(new SystemMessages.StatusMessage(this, SystemMessages.StatusMessageType.Error, message));

    public void Receive(NodeMessages.SelectedNodeChanged message) => SelectedNode = message.Value;
    public void Receive(DatabaseMessages.SelectedDatabseChanged message) => SelectedDatabase = message.Value;
    public void Receive(ScannerMessages.SelectedScannerChanged message) => SelectedScanner = message.Value;

    public async Task<MessageDialogResult> OkCancelDialog(string title, string message)
    {

        var result = await DialogCoordinator.ShowMessageAsync(this, title, message, MessageDialogStyle.AffirmativeAndNegative);

        return result;
    }

    private void GetImage(string imagePath)
    {
        SourceImage = File.ReadAllBytes(imagePath);
        SourceImageUID = ImageUtilities.ImageUID(SourceImage);
    }
    private void GetStored()
    {
        V275GetStored();
        V5GetStored();
        L95xxGetStored();
    }

    [RelayCommand]
    private void Save(string type)
    {
        SendTo95xxApplication();

        var path = GetSaveFilePath();
        if (string.IsNullOrEmpty(path)) return;
        try
        {
            if (type == "v275stored")
            {
                var bmp = ImageUtilities.ConvertToBmp(V275Image);
                _ = SaveImageBytesToFile(path, bmp);
                Clipboard.SetText(path);
            }
            else if (type == "v5Stored")
            {
                var bmp = ImageUtilities.ConvertToBmp(V5Image);
                _ = SaveImageBytesToFile(path, bmp);
                Clipboard.SetText(path);
            }
            else
            {
                var bmp = ImageUtilities.ConvertToBmp(SourceImage, 600);
                _ = SaveImageBytesToFile(path, bmp);
                Clipboard.SetText(path);
            }
        }
        catch (Exception)
        {

        }
    }
    [RelayCommand]
    private async Task Store(string device)
    {
        if (device == "V275")
        {
            if (V275StoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            _ = SelectedDatabase.InsertOrReplace_V275Result(new Databases.ImageResults.V275Result
            {
                ImageRollUID = SelectedImageRoll.UID,
                SourceImageUID = SourceImageUID,
                SourceImage = SourceImage,
                Template = JsonConvert.SerializeObject(V275CurrentTemplate),
                Report = JsonConvert.SerializeObject(V275CurrentReport),
                StoredImage = V275Image
            });

            V275CurrentSectors.Clear();

            V275GetStored();
        }
        else if (device == "V5")
        {
            if (V5StoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            _ = SelectedDatabase.InsertOrReplace_V5Result(new Databases.ImageResults.V5Result
            {
                ImageRollUID = SelectedImageRoll.UID,
                SourceImageUID = SourceImageUID,
                SourceImage = SourceImage,
                Report = JsonConvert.SerializeObject(V5CurrentReport),
                StoredImage = V5Image
            });

            V5CurrentSectors.Clear();

            V5GetStored();
        }
        else if (device == "L95xx")
        {
            if (L95xxStoredSectors.Count > 0)
                if (await OkCancelDialog("Overwrite Stored Sectors", $"Are you sure you want to overwrite the stored sectors for this image?\r\nThis can not be undone!") != MessageDialogResult.Affirmative)
                    return;

            var temp = new List<L95xxReport>();
            foreach (var sec in L95xxCurrentSectors)
                temp.Add(new L95xxReport() { Report = sec.L95xxPacket, Template = sec.Template });

            _ = SelectedDatabase.InsertOrReplace_L95xxResult(new Databases.ImageResults.L95xxResult
            {
                ImageRollUID = SelectedImageRoll.UID,
                SourceImageUID = SourceImageUID,
                SourceImage = SourceImage,
                Report = JsonConvert.SerializeObject(temp),
                //StoredImage = L95xxImage
            });

            L95xxCurrentSectors.Clear();

            L95xxGetStored();
        }
    }
    [RelayCommand]
    private async Task ClearStored(string device)
    {
        if (await OkCancelDialog("Clear Stored Sectors", $"Are you sure you want to clear the stored sectors for this image?\r\nThis can not be undone!") == MessageDialogResult.Affirmative)
        {
            if (device == "V275")
            {
                _ = SelectedDatabase.Delete_V275Result(SelectedImageRoll.UID, SourceImageUID);
                V275GetStored();
            }
            else if (device == "V5")
            {
                _ = SelectedDatabase.Delete_V5Result(SelectedImageRoll.UID, SourceImageUID);
                V5GetStored();
            }
            else if (device == "L95xx")
            {
                _ = SelectedDatabase.Delete_L95xxResult(SelectedImageRoll.UID, SourceImageUID);
                L95xxGetStored();
            }
        }
    }
    [RelayCommand]
    private void ClearRead(string device)
    {
        if (device == "V275")
        {
            V275CurrentReport = null;
            V275CurrentTemplate = null;

            V275CurrentSectors.Clear();
            V275DiffSectors.Clear();

            V275ResultRow = SelectedDatabase.Select_V275Result(SelectedImageRoll.UID, SourceImageUID);

            if (V275ResultRow == null)
            {
                V275Image = null;
                V275SectorsImageOverlay = null;
                IsV275ImageStored = false;
            }
            else
            {
                V275Image = V275ResultRow.StoredImage;
                V275SectorsImageOverlay = V275CreateSectorsImageOverlay(JsonConvert.DeserializeObject<Job>(V275ResultRow.Template), false);
                IsV275ImageStored = true;
            }
        }
        else if (device == "V5")
        {
            V5CurrentReport = null;

            V5CurrentSectors.Clear();
            V5DiffSectors.Clear();

            V5ResultRow = SelectedDatabase.Select_V5Result(SelectedImageRoll.UID, SourceImageUID);

            if (V5ResultRow == null)
            {
                V5Image = null;
                V5SectorsImageOverlay = null;
                IsV5ImageStored = false;
            }
            else
            {
                V5Image = V5ResultRow.StoredImage;
                V5SectorsImageOverlay = V5CreateSectorsImageOverlay(JsonConvert.DeserializeObject<JObject>(V5ResultRow.Report));
                IsV5ImageStored = true;
            }
        }
        else if (device == "L95xx")
        {
            L95xxCurrentReport = null;

            L95xxCurrentSectors.Clear();
            L95xxDiffSectors.Clear();

            L95xxResultRow = SelectedDatabase.Select_L95xxResult(SelectedImageRoll.UID, SourceImageUID);

            if (L95xxResultRow == null)
            {
                L95xxImage = null;
                L95xxSectorsImageOverlay = null;
                IsL95xxImageStored = false;
            }
            else
            {
                //L95xxImage = L95xxResultRow.StoredImage;
                //L95xxSectorsImageOverlay = L95xxCreateSectorsImageOverlay(true);
                //IsL95xxImageStored = true;
            }
        }
    }


    [RelayCommand] private void RedoFiducial() => ImageUtilities.RedrawFiducial(SourceImagePath, false);

    //const UInt32 WM_KEYDOWN = 0x0100;
    //const int VK_F5 = 0x74;

    //[DllImport("user32.dll")]
    //static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

    private void SendTo95xxApplication() => _ = Process.GetProcessesByName("LVS-95XX");//foreach (Process proc in processes)//    PostMessage(proc.MainWindowHandle, WM_KEYDOWN, VK_F5, 0);
    private string GetSaveFilePath()
    {
        var saveFileDialog1 = new SaveFileDialog();
        saveFileDialog1.Filter = "Bitmap Image|*.bmp";//|Gif Image|*.gif|JPeg Image|*.jpg";
        saveFileDialog1.Title = "Save an Image File";
        _ = saveFileDialog1.ShowDialog();

        return saveFileDialog1.FileName;
    }
    private string SaveImageBytesToFile(string path, byte[] img)
    {
        File.WriteAllBytes(path, img);

        return "";
    }




    //public void Clear()
    //{
    //    SourceImage = null;
    //    V275Image = null;
    //    V275CurrentTemplate = null;

    //    V275StoredTemplate = null;

    //    foreach (var sec in V275CurrentSectors)
    //        sec.Clear();

    //    V275CurrentSectors.Clear();

    //    foreach (var sec in V275StoredSectors)
    //        sec.Clear();

    //    V275StoredSectors.Clear();

    //    dialogCoordinator = null;
    //    ImageResultsDatabase = null;
    //    V275 = null;
    //}
}
