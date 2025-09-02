using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LabelVal.Results.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class FileFolderEntry : ObservableObject
{
    public string Name { get; private set; }

    private string path;
    [JsonProperty]
    public string Path
    {
        get => path;
        set
        {
            path = value;
            if (System.IO.File.Exists(value))
            {
                IsFile = true;
                Name = GetName(value);
            }
            else if (System.IO.Directory.Exists(value))
            {
                IsDirectory = true;
                Name = GetName(value);
            }
            else
                if (System.IO.Path.GetFileName(path) != "")
            {
                IsFile = true;
                Name = GetName(value);
            }
        }
    }
    public bool IsDirectory { get; private set; }
    public bool IsFile { get; private set; }

    [JsonProperty][ObservableProperty] public bool isSelected = true;
    partial void OnIsSelectedChanged(bool value)
    {
        foreach (var child in this.Children)
            child.IsSelected = value;
    }

    [JsonProperty]
    public ObservableCollection<FileFolderEntry> Children { get; } = [];

    public FileFolderEntry(string path) => Path = path;

    private string GetName(string path)
    {
        try
        {
            if (IsFile)
                return System.IO.Path.GetFileNameWithoutExtension(Path);
            else
                return path.Substring(path.LastIndexOf('\\') + 1);
        }
        catch (Exception)
        {
            return "";
        }
    }
}
