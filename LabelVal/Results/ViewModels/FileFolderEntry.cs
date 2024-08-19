using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace LabelVal.Results.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public partial class FileFolderEntry : List<FileFolderEntry>, INotifyPropertyChanged
{ 
    public event PropertyChangedEventHandler PropertyChanged;
    private void OnPropertyChanged(string propertyName) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

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
        }
    }
    public bool IsDirectory { get; private set; }
    public bool IsFile { get; private set; }

    private bool isSelected = true;

    [JsonProperty] public bool IsSelected { get => isSelected; set => OnIsSelectedChanged(value); }
    private void OnIsSelectedChanged(bool value)
    {
        isSelected = value;
        foreach (var child in this)
            child.IsSelected = value;

        OnPropertyChanged("IsSelected");
    }

    //[JsonProperty]
    //public ObservableCollection<FileFolderEntry> Children { get; } = [];

    public FileFolderEntry(string path) => Path = path;

    private string GetName(string path)
    {
        if (IsFile)
            return System.IO.Path.GetFileNameWithoutExtension(Path);
        else
            return System.IO.Path.GetDirectoryName(path.TrimEnd(System.IO.Path.DirectorySeparatorChar));
    }
}
