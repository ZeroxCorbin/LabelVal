using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.Models;
public partial class ImageEntry : ObservableObject
{
    public string Name
    {
        get => string.IsNullOrEmpty(name) ? System.IO.Path.GetFileName(Path) : name;
        set => SetProperty(ref name, value);
    }
    private string name;

    public string Path { get; set; }

    public byte[] Image { get; set; }
    public string UID => ImageUtilities.ImageUID(Image);

}
