using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using Newtonsoft.Json;
using SQLite;
using System.Collections.Generic;

namespace LabelVal.Results.Databases;
public partial class ImageResult : ObservableObject
{
    [Indexed(Name = "CompositeKey", Order = 1, Unique = true)]
    public string SourceImageUID {get; set;}

    [Indexed(Name = "CompositeKey", Order = 2, Unique = true)]
    public string ImageRollUID { get; set;}

    public string RunUID { get; set; }

    [ObservableProperty] private string sourceImage;
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

    [SQLite.Ignore] public ImageEntry Source => !string.IsNullOrEmpty(SourceImage) ? JsonConvert.DeserializeObject<ImageEntry>(SourceImage) : null;
    [SQLite.Ignore] public ImageEntry Stored => !string.IsNullOrEmpty(StoredImage) ? JsonConvert.DeserializeObject<ImageEntry>(StoredImage) : null;
}
public class V275Result : ImageResult
{
    [SQLite.Ignore][JsonIgnore] public V275_REST_Lib.Models.Job _Job => !string.IsNullOrEmpty(Template) ? JsonConvert.DeserializeObject<V275_REST_Lib.Models.Job>(Template) : null;
    [SQLite.Ignore][JsonIgnore] public V275_REST_Lib.Models.Report _Report => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<V275_REST_Lib.Models.Report>(Report): null;
}
public class V5Result : ImageResult
{
    [SQLite.Ignore][JsonIgnore] public V5_REST_Lib.Models.Config _Config => !string.IsNullOrEmpty(Template) ? JsonConvert.DeserializeObject<V5_REST_Lib.Models.Config>(Template) : null;
    [SQLite.Ignore][JsonIgnore] public V5_REST_Lib.Models.ResultsAlt _Report => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<V5_REST_Lib.Models.ResultsAlt>(Report) : null;
    [SQLite.Ignore][JsonIgnore] public V5_REST_Lib.Models.Results _ReportOld => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<V5_REST_Lib.Models.Results>(Report) : null;
}

public class L95xxResult : ImageResult 
{
    [SQLite.Ignore][JsonIgnore] public Dictionary<string, object> _Settings => !string.IsNullOrEmpty(Template) ? JsonConvert.DeserializeObject<Dictionary<string, object>>(Template) : null;
    [SQLite.Ignore][JsonIgnore] public List<Lvs95xx.lib.Core.Controllers.FullReport> _Report => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<List<Lvs95xx.lib.Core.Controllers.FullReport>>(Report) : null;
}
