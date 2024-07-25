using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Messages;
using Mysqlx.Crud;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageRolls : ObservableRecipient
{
    public ObservableCollection<ImageRollEntry> FixedImageRolls { get; } = [];
    public ObservableCollection<ImageRollEntry> UserImageRolls { get; } = [];

    [ObservableProperty] private ImageRollEntry newImageRoll = null;


     [ObservableProperty][NotifyPropertyChangedRecipients] private ImageRollEntry selectedImageRoll;// = App.Settings.GetValue<ImageRollEntry>(nameof(SelectedImageRoll), null);
    partial void OnSelectedImageRollChanged(ImageRollEntry value) { App.Settings.SetValue(nameof(SelectedImageRoll), value); if (value != null) SelectedUserImageRoll = null; }

    [ObservableProperty][NotifyPropertyChangedRecipients] private ImageRollEntry selectedUserImageRoll;// = App.Settings.GetValue<ImageRollEntry>(nameof(SelectedUserImageRoll), null);
    partial void OnSelectedUserImageRollChanged(ImageRollEntry value) { if(value != null) App.Settings.SetValue(nameof(SelectedUserImageRoll), value); if (value != null) SelectedImageRoll = null; }

    private Databases.ImageRollsDatabase ImageRollsDatabase { get; } = new Databases.ImageRollsDatabase();

    public ImageRolls()
    {
        WeakReferenceMessenger.Default.Register<RequestMessage<ImageRollEntry>>(
        this,
        (recipient, message) =>
        {
            message.Reply(SelectedImageRoll ?? SelectedUserImageRoll);
        });

        LoadFixedImageRollsList();

        ImageRollsDatabase.Open(App.ImageRollsDatabasePath);

        LoadUserImageRollsList();

        IsActive = true;
    }

    private void LoadFixedImageRollsList()
    {
        LogInfo($"Loading image rolls from file system. {App.AssetsImageRollRoot}");

        FixedImageRolls.Clear();

        foreach (var dir in Directory.EnumerateDirectories(App.AssetsImageRollRoot).ToList().OrderBy((e) => Regex.Replace(e, "[0-9]+", match => match.Value.PadLeft(10, '0'))))
        {
            var fnd = dir[(dir.LastIndexOf('\\') + 1)..];
            LogDebug($"Found: {fnd}");

            foreach (var subdir in Directory.EnumerateDirectories(dir))
            {
                var files = Directory.EnumerateFiles(subdir, "*.imgr").ToList();
                if (files.Count == 0)
                    continue;

                try
                {
                    var imgr = JsonConvert.DeserializeObject<ImageRollEntry>(File.ReadAllText(files.First()));
                    imgr.Path = subdir;
                    FixedImageRolls.Add(imgr);
                }
                catch (Exception ex)
                {
                    LogError($"Failed to load image roll from {files.First()}", ex);
                    continue;
                }
            }
        }

        LogInfo($"Processed {FixedImageRolls.Count} fixed image rolls.");
    }
    private void LoadUserImageRollsList()
    {
        LogInfo($"Loading user image rolls from database. {App.ImageRollsDatabasePath}");

        UserImageRolls.Clear();

        foreach (var roll in ImageRollsDatabase.SelectAllImageRolls())
        {
            LogDebug($"Found: {roll.Name}");

            roll.ImageRollsDatabase = ImageRollsDatabase;
            UserImageRolls.Add(roll);
        }

        LogInfo($"Processed {UserImageRolls.Count} user image rolls.");
    }

    [RelayCommand]
    private void Add()
    {
        LogInfo("Adding image roll.");

        NewImageRoll = new ImageRollEntry
        {
            ImageRollsDatabase = ImageRollsDatabase
        };
    }

    [RelayCommand]
    private void Edit()
    {
        LogInfo("Editing image roll.");

        NewImageRoll = SelectedUserImageRoll.CopyLite();
    }

    [RelayCommand]
    public void Save()
    {
        if (string.IsNullOrEmpty(NewImageRoll.Name))
            return;

        if (NewImageRoll.SelectedStandard is Sectors.Interfaces.StandardsTypes.GS1 &&
            NewImageRoll.SelectedGS1Table is Sectors.Interfaces.GS1TableNames.None or Sectors.Interfaces.GS1TableNames.Unsupported)
            return;

        if (ImageRollsDatabase == null)
            return;

        if (ImageRollsDatabase.InsertOrReplaceImageRoll(NewImageRoll) > 0)
        {
            LogInfo($"Saved image roll: {NewImageRoll.Name}");

            LoadUserImageRollsList();
            NewImageRoll = null;
        }
        else
            LogError($"Failed to save image roll: {NewImageRoll.Name}");
    }

    [RelayCommand]
    public void Delete()
    {
        if (ImageRollsDatabase == null || SelectedUserImageRoll == null)
            return;

        foreach (var img in SelectedUserImageRoll.Images)
        {
            if (ImageRollsDatabase.DeleteImage(img.UID))
                LogInfo($"Deleted image: {img.UID}");
            else
                LogError($"Failed to delete image: {img.UID}");
        }

        if (ImageRollsDatabase.DeleteImageRoll(NewImageRoll.UID))
        {
            LogInfo($"Deleted image roll: {NewImageRoll.UID}");

            LoadUserImageRollsList();
            NewImageRoll = null;
            SelectedUserImageRoll = null;
        }
        else
            LogError($"Failed to delete image roll: {NewImageRoll.UID}");
    }

    [RelayCommand]
    public void Cancel() => NewImageRoll = null;

    [RelayCommand]
    private void UIDToClipboard()
    {
        Clipboard.SetText(Guid.NewGuid().ToString());
    }

    #region Logging
    private readonly Logging.Logger logger = new();
    public void LogInfo(string message) => logger.LogInfo(this.GetType(), message);
    public void LogDebug(string message) => logger.LogDebug(this.GetType(), message);
    public void LogWarning(string message) => logger.LogInfo(this.GetType(), message);
    public void LogError(string message) => logger.LogError(this.GetType(), message);
    public void LogError(Exception ex) => logger.LogError(this.GetType(), ex);
    public void LogError(string message, Exception ex) => logger.LogError(this.GetType(), message, ex);
    #endregion
}
