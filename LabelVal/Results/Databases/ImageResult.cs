using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Results.ViewModels;
using Lvs95xx.lib.Core.Controllers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;

namespace LabelVal.Results.Databases;
public partial class Result : ObservableObject
{
    [Indexed(Name = "CompositeKey", Order = 1, Unique = true)]
    public string SourceImageUID { get; set; }

    [Indexed(Name = "CompositeKey", Order = 2, Unique = true)]
    public string ImageRollUID { get; set; }
    [Indexed(Name = "CompositeKey", Order = 3, Unique = true)]
    public string RunUID { get; set; }

    [Indexed(Name = "CompositeKey", Order = 4, Unique = true)]
    public ImageResultEntryDevices Device { get; set; }

    /// <see cref="SourceImage"/>
    [ObservableProperty] private string sourceImage;
    /// <see cref="StoredImage"/>
    [ObservableProperty] private string storedImage;

    /// <summary>
    /// This is the serialized version of the Config/Job/Settings collected from the specific device.
    /// <see cref="Template"/>
    /// </summary>
    [ObservableProperty] private string templateString;
    [SQLite.Ignore][JsonIgnore] public JObject Template { get => !string.IsNullOrEmpty(TemplateString) ? JsonConvert.DeserializeObject<JObject>(TemplateString) : null; set => TemplateString = JsonConvert.SerializeObject(value); }

    /// <summary>
    /// This is the serialized version of the Report collected from the specific device.
    /// <see cref="Report"/>
    /// </summary>
    [ObservableProperty] private string reportString;
    [SQLite.Ignore][JsonIgnore] public JObject Report { get => !string.IsNullOrEmpty(ReportString) ? JsonConvert.DeserializeObject<JObject>(ReportString) : null; set => ReportString = JsonConvert.SerializeObject(value); }

    [ObservableProperty] private string version;

    [SQLite.Ignore] public ImageEntry Source { get => !string.IsNullOrEmpty(SourceImage) ? JsonConvert.DeserializeObject<ImageEntry>(SourceImage) : null; set { SourceImage = JsonConvert.SerializeObject(value); SourceImageUID = value.UID; }  }
    [SQLite.Ignore] public ImageEntry Stored { get => !string.IsNullOrEmpty(StoredImage) ? JsonConvert.DeserializeObject<ImageEntry>(StoredImage) : null; set { StoredImage = JsonConvert.SerializeObject(value); } }
}

public class L95xxResult : Result
{
    [SQLite.Ignore][JsonIgnore] public List<FullReport> _AllSectors { get => !string.IsNullOrEmpty(ReportString) ? JsonConvert.DeserializeObject<List<FullReport>>(ReportString) : null; set => ReportString = JsonConvert.SerializeObject(value); }
}
