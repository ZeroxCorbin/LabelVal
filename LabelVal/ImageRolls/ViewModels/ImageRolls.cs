using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;
using Newtonsoft.Json;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace LabelVal.ImageRolls.ViewModels;
public partial class ImageRolls : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    public ObservableCollection<ImageRollEntry> FixedImageRolls { get; } = [];
    public ObservableCollection<ImageRollEntry> UserImageRolls { get; } = [];


    [ObservableProperty] private ImageRollEntry selectedImageRoll = App.Settings.GetValue<ImageRollEntry>(nameof(SelectedImageRoll), null);
    partial void OnSelectedImageRollChanged(ImageRollEntry value) => App.Settings.SetValue(nameof(SelectedImageRoll), value);
    partial void OnSelectedImageRollChanged(ImageRollEntry oldValue, ImageRollEntry newValue) => _ = WeakReferenceMessenger.Default.Send(new ImageRollMessages.SelectedImageRollChanged(newValue, oldValue));

    public ImageRolls()
    {
        LoadImageRollsList();
       // SelectImageRoll();
    }

    private void LoadImageRollsList()
    {
        Logger.Info("Loading image rolls from file system. {path}", App.AssetsImageRollRoot);

        UserImageRolls.Clear();
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
    private void SelectImageRoll()
    {
        ImageRollEntry std;
        if (SelectedImageRoll != null && (std = UserImageRolls.FirstOrDefault((e) => e.Name.Equals(SelectedImageRoll.Name))) != null)
            SelectedImageRoll = std;
        else if (UserImageRolls.Count > 0)
            SelectedImageRoll = UserImageRolls.First();
    }
}
