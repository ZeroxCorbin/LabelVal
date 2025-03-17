using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SQLite;

namespace LabelVal.Results.Databases;
public partial class ImageResult : ObservableObject
{

    //[Indexed(Name = "CompositeKey", Order = 1, Unique = true)]
    [PrimaryKey] public string SourceImageUID { get; set; }

    //[Indexed(Name = "CompositeKey", Order = 2, Unique = true)]
    public string ImageRollUID { get; set; }

    public string RunUID { get; set; }

    /// <see cref="SourceImage"/>
    [ObservableProperty] private string sourceImage;
    /// <see cref="StoredImage"/>
    [ObservableProperty] private string storedImage;

    /// <summary>
    /// This is the serialized version of the Config/Job/Settings collected from the specific device.
    /// <see cref="Template"/>
    /// </summary>
    [ObservableProperty] private string template;
    /// <summary>
    /// This is the serialized version of the Report collected from the specific device.
    /// <see cref="Report"/>
    /// </summary>
    [ObservableProperty] private string report;

    [ObservableProperty] private string version;

    [SQLite.Ignore] public ImageEntry Source { get => !string.IsNullOrEmpty(SourceImage) ? JsonConvert.DeserializeObject<ImageEntry>(SourceImage) : null; set { SourceImage = JsonConvert.SerializeObject(value); SourceImageUID = value.UID; }  }
    [SQLite.Ignore] public ImageEntry Stored { get => !string.IsNullOrEmpty(StoredImage) ? JsonConvert.DeserializeObject<ImageEntry>(StoredImage) : null; set { StoredImage = JsonConvert.SerializeObject(value); } }
}
public class V275Result : ImageResult
{
    [SQLite.Ignore][JsonIgnore] public JObject _Job { get=> !string.IsNullOrEmpty(Template) ? JsonConvert.DeserializeObject<JObject>(Template) : null; set => Template = JsonConvert.SerializeObject(value); }
    [SQLite.Ignore][JsonIgnore] public JObject _Report { get => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<JObject>(Report) : null; set => Report = JsonConvert.SerializeObject(value); }
}
public class V5Result : ImageResult
{
    [SQLite.Ignore][JsonIgnore] public JObject _Config {get=>!string.IsNullOrEmpty(Template) ? JsonConvert.DeserializeObject<JObject>(Template) : null; set => Template = JsonConvert.SerializeObject(value); }
    [SQLite.Ignore][JsonIgnore] public JObject _Report {get=> !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<JObject>(Report) : null; set => Report = JsonConvert.SerializeObject(value); }
}

public class L95xxResult : ImageResult
{
    [SQLite.Ignore][JsonIgnore] public List<Lvs95xx.lib.Core.Models.Setting> _Settings { get => !string.IsNullOrEmpty(Template) ? JsonConvert.DeserializeObject<List<Lvs95xx.lib.Core.Models.Setting>>(Template) : null; set => Template = JsonConvert.SerializeObject(value); }
    [SQLite.Ignore][JsonIgnore] public List<Lvs95xx.lib.Core.Controllers.FullReport> _Report { get => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<List<Lvs95xx.lib.Core.Controllers.FullReport>>(Report) : null; set => Report = JsonConvert.SerializeObject(value); }
}
