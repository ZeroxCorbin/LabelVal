using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Xml.Linq;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageRolls : ObservableRecipient, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public ObservableCollection<ImageRollEntry> FixedImageRolls { get; } = [];
    public ObservableCollection<ImageRollEntry> UserImageRolls { get; } = [];

    [ObservableProperty] private ImageRollEntry userImageRoll = null;


    [ObservableProperty][NotifyPropertyChangedRecipients] private ImageRollEntry selectedImageRoll = App.Settings.GetValue<ImageRollEntry>(nameof(SelectedImageRoll), null);
    partial void OnSelectedImageRollChanged(ImageRollEntry value) { App.Settings.SetValue(nameof(SelectedImageRoll), value); if (value != null) SelectedUserImageRoll = null; }


    [ObservableProperty][NotifyPropertyChangedRecipients] private ImageRollEntry selectedUserImageRoll = App.Settings.GetValue<ImageRollEntry>(nameof(SelectedUserImageRoll), null);
    partial void OnSelectedUserImageRollChanged(ImageRollEntry value) { App.Settings.SetValue(nameof(SelectedUserImageRoll), value); if (value != null) SelectedImageRoll = null; }


    private PrinterSettings selectedPrinter;

    private Databases.ImageRolls ImageRollsDatabase { get; } = new Databases.ImageRolls();

    public ImageRolls(PrinterSettings selectedPrinter)
    {
        this.selectedPrinter = selectedPrinter;

        LoadFixedImageRollsList();

        ImageRollsDatabase.Open(App.ImageRollsDatabasePath);
        LoadUserImageRollsList();

        IsActive = true;
        // SelectImageRoll();
    }

    public void Receive(PropertyChangedMessage<PrinterSettings> message) => selectedPrinter = message.NewValue;

    private void LoadFixedImageRollsList()
    {
        Logger.Info("Loading image rolls from file system. {path}", App.AssetsImageRollRoot);

        FixedImageRolls.Clear();

        foreach (var dir in Directory.EnumerateDirectories(App.AssetsImageRollRoot).ToList().OrderBy((e) => Regex.Replace(e, "[0-9]+", match => match.Value.PadLeft(10, '0'))))
        {
            Logger.Debug("Found: {name}", dir[(dir.LastIndexOf("\\") + 1)..]);

            foreach (var subdir in Directory.EnumerateDirectories(dir))
            {
                var files = Directory.EnumerateFiles(subdir, "*.imgr").ToList();
                if (files.Count == 0)
                    continue;

                try
                {
                    var imgr = JsonConvert.DeserializeObject<ImageRollEntry>(File.ReadAllText(files.First()));
                    imgr.Path = subdir;
                    imgr.SelectedPrinter = selectedPrinter;
                    FixedImageRolls.Add(imgr);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Failed to load image roll from {path}", files.First());
                    continue;
                }
                // FixedImageRolls.Add(new ImageRollEntry(dir[(dir.LastIndexOf("\\") + 1)..], subdir));
            }
        }

        Logger.Info("Processed {count} fixed image rolls.", FixedImageRolls.Count);

        //foreach (var dir in Directory.EnumerateDirectories(App.ImageRollRoot).ToList().OrderBy((e) => Regex.Replace(e, "[0-9]+", match => match.Value.PadLeft(10, '0'))))
        //{
        //    Logger.Debug("Found: {name}", dir[(dir.LastIndexOf("\\") + 1)..]);

        //    foreach (var subdir in Directory.EnumerateDirectories(dir))
        //    {
        //        UserImageRolls.Add(new ImageRollEntry(dir[(dir.LastIndexOf("\\") + 1)..], subdir));
        //    }
        //}

        //Logger.Info("Processed {count} image rolls.", UserImageRolls.Count);
    }

    private void LoadUserImageRollsList()
    {
        Logger.Info("Loading image rolls from database. {path}", App.AssetsImageRollRoot);

        UserImageRolls.Clear();

        foreach (var roll in ImageRollsDatabase.SelectAllImageRolls())
        {
            roll.ImageRollsDatabase = ImageRollsDatabase;
            UserImageRolls.Add(roll);
        }



        Logger.Info("Processed {count} user image rolls.", UserImageRolls.Count);

        //foreach (var dir in Directory.EnumerateDirectories(App.ImageRollRoot).ToList().OrderBy((e) => Regex.Replace(e, "[0-9]+", match => match.Value.PadLeft(10, '0'))))
        //{
        //    Logger.Debug("Found: {name}", dir[(dir.LastIndexOf("\\") + 1)..]);

        //    foreach (var subdir in Directory.EnumerateDirectories(dir))
        //    {
        //        UserImageRolls.Add(new ImageRollEntry(dir[(dir.LastIndexOf("\\") + 1)..], subdir));
        //    }
        //}

        //Logger.Info("Processed {count} image rolls.", UserImageRolls.Count);
    }


    [RelayCommand]
    private void Add()
    {
        Logger.Info("Adding image roll.");

        UserImageRoll = new ImageRollEntry();
        UserImageRoll.SelectedPrinter = selectedPrinter;
        UserImageRoll.ImageRollsDatabase = ImageRollsDatabase;
    }

    [RelayCommand]
    private void Edit()
    {
        Logger.Info("Editing image roll.");

        UserImageRoll = SelectedUserImageRoll.CopyLite();
    }


    [RelayCommand]
    public void Save()
    {
        if (string.IsNullOrEmpty(UserImageRoll.Name))
            return;

        if (UserImageRoll.SelectedStandard is Sectors.ViewModels.StandardsTypes.GS1 &&
            UserImageRoll.SelectedGS1Table is Sectors.ViewModels.GS1TableNames.None or Sectors.ViewModels.GS1TableNames.Unsupported)
            return;

        if (ImageRollsDatabase == null)
            return;

        if (ImageRollsDatabase.InsertOrReplaceImageRoll(UserImageRoll) > 0)
        {
            Logger.Info("Saved image roll: {name}", UserImageRoll.Name);
            LoadUserImageRollsList();
            UserImageRoll = null;
        }
        else
            Logger.Error("Failed to save image roll: {name}", UserImageRoll.Name);
    }

    [RelayCommand]
    public void Delete()
    {
        if (ImageRollsDatabase == null)
            return;

        foreach (var img in SelectedUserImageRoll.Images)
        {
            if (ImageRollsDatabase.DeleteImage(img.UID))
                Logger.Info("Deleted image: {UID}", img.UID);
            else
                Logger.Error("Failed to delete image: {UID}", img.UID);
        }

        if (ImageRollsDatabase.DeleteImageRoll(UserImageRoll.UID))
        {
            Logger.Info("Deleted image roll: {UID}", UserImageRoll.UID);
            LoadUserImageRollsList();
            UserImageRoll = null;
            SelectedUserImageRoll = null;
        }
        else
            Logger.Error("Failed to delete image roll: {UID}", UserImageRoll.UID);
    }


    [RelayCommand]
    public void Cancel() => UserImageRoll = null;

    [RelayCommand]
    private void UIDToClipboard() => Clipboard.SetText(Guid.NewGuid().ToString());
}
