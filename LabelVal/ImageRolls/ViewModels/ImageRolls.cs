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

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageRolls : ObservableRecipient, IRecipient<PropertyChangedMessage<PrinterSettings>>
{
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
    }

    public void Receive(PropertyChangedMessage<PrinterSettings> message)
    {
        selectedPrinter = message.NewValue;
    }

    private void LoadFixedImageRollsList()
    {

        UpdateStatus($"Loading image rolls from file system. {App.AssetsImageRollRoot}");

        FixedImageRolls.Clear();

        foreach (var dir in Directory.EnumerateDirectories(App.AssetsImageRollRoot).ToList().OrderBy((e) => Regex.Replace(e, "[0-9]+", match => match.Value.PadLeft(10, '0'))))
        {
            var fnd = dir[(dir.LastIndexOf('\\') + 1)..];
            UpdateStatus($"Found: {fnd}", SystemMessages.StatusMessageType.Debug);

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
                    UpdateStatus($"Failed to load image roll from {files.First()}", ex);
                    continue;
                }
            }
        }

        UpdateStatus($"Processed {FixedImageRolls.Count} fixed image rolls.");
    }

    private void LoadUserImageRollsList()
    {
        UpdateStatus($"Loading image rolls from database. {App.AssetsImageRollRoot}");

        UserImageRolls.Clear();

        foreach (var roll in ImageRollsDatabase.SelectAllImageRolls())
        {
            roll.ImageRollsDatabase = ImageRollsDatabase;
            UserImageRolls.Add(roll);
        }

        UpdateStatus($"Processed {UserImageRolls.Count} user image rolls.");
    }

    [RelayCommand]
    private void Add()
    {
        UpdateStatus("Adding image roll.");

        UserImageRoll = new ImageRollEntry
        {
            SelectedPrinter = selectedPrinter,
            ImageRollsDatabase = ImageRollsDatabase
        };
    }

    [RelayCommand]
    private void Edit()
    {
        UpdateStatus("Editing image roll.");

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
            UpdateStatus($"Saved image roll: {UserImageRoll.Name}");

            LoadUserImageRollsList();
            UserImageRoll = null;
        }
        else
            UpdateStatus($"Failed to save image roll: {UserImageRoll.Name}", SystemMessages.StatusMessageType.Error);
    }

    [RelayCommand]
    public void Delete()
    {
        if (ImageRollsDatabase == null)
            return;

        foreach (var img in SelectedUserImageRoll.Images)
        {
            if (ImageRollsDatabase.DeleteImage(img.UID))
                UpdateStatus($"Deleted image: {img.UID}");
            else
                UpdateStatus($"Failed to delete image: {img.UID}", SystemMessages.StatusMessageType.Error);
        }

        if (ImageRollsDatabase.DeleteImageRoll(UserImageRoll.UID))
        {
            UpdateStatus($"Deleted image roll: {UserImageRoll.UID}");

            LoadUserImageRollsList();
            UserImageRoll = null;
            SelectedUserImageRoll = null;
        }
        else
            UpdateStatus($"Failed to delete image roll: {UserImageRoll.UID}");
    }

    [RelayCommand]
    public void Cancel()
    {
        UserImageRoll = null;
    }

    [RelayCommand]
    private void UIDToClipboard()
    {
        Clipboard.SetText(Guid.NewGuid().ToString());
    }

    #region Logging & Status Messages

    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private void UpdateStatus(string message)
    {
        Logger.Info(message);
        _ = Messenger.Send(new SystemMessages.StatusMessage(message, SystemMessages.StatusMessageType.Info));
    }
    private void UpdateStatus(string message, SystemMessages.StatusMessageType type)
    {
        switch (type)
        {
            case SystemMessages.StatusMessageType.Info:
                Logger.Info(message);
                break;
            case SystemMessages.StatusMessageType.Debug:
                Logger.Debug(message);
                break;
            case SystemMessages.StatusMessageType.Warning:
                Logger.Warn(message);
                break;
            case SystemMessages.StatusMessageType.Error:
                Logger.Error(message);
                break;
            default:
                Logger.Info(message);
                break;
        }
        _ = Messenger.Send(new SystemMessages.StatusMessage(message, type));
    }
    private void UpdateStatus(Exception ex)
    {
        Logger.Error(ex);
        _ = Messenger.Send(new SystemMessages.StatusMessage(ex));
    }
    private void UpdateStatus(string message, Exception ex)
    {
        Logger.Error(ex);
        _ = Messenger.Send(new SystemMessages.StatusMessage(ex));
    }

    #endregion
}
