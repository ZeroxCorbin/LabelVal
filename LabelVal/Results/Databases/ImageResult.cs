using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace LabelVal.Results.Databases;
public partial class ImageResult : ObservableObject
{
    [SQLite.PrimaryKey] public string SourceImageUID {get; set;}
    public string ImageRollUID { get; set;}
    public string RunUID { get; set; }

    [ObservableProperty] private string sourceImage;
    [ObservableProperty] private string storedImage;

    [ObservableProperty] private string template;
    [ObservableProperty] private string report;

    [SQLite.Ignore] public ImageEntry Source => !string.IsNullOrEmpty(SourceImage) ? JsonConvert.DeserializeObject<ImageEntry>(SourceImage) : null;
    [SQLite.Ignore] public ImageEntry Stored => !string.IsNullOrEmpty(StoredImage) ? JsonConvert.DeserializeObject<ImageEntry>(StoredImage) : null;
}
public class V275Result : ImageResult
{
    [SQLite.Ignore] public V275_REST_lib.Models.Job _Job => !string.IsNullOrEmpty(Template) ? JsonConvert.DeserializeObject<V275_REST_lib.Models.Job>(Template) : null;
    [SQLite.Ignore] public V275_REST_lib.Models.Report _Report => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<V275_REST_lib.Models.Report>(Report): null;
}
public class V5Result : ImageResult
{
    [SQLite.Ignore] public V5_REST_Lib.Models.Config _Config => !string.IsNullOrEmpty(Template) ? JsonConvert.DeserializeObject<V5_REST_Lib.Models.Config>(Template) : null;
    [SQLite.Ignore] public V5_REST_Lib.Models.ResultsAlt _Report => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<V5_REST_Lib.Models.ResultsAlt>(Report) : null;
}

public class L95xxResult : ImageResult 
{
     [SQLite.Ignore] public List<LabelVal.LVS_95xx.Models.FullReport> _Report => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<List<LabelVal.LVS_95xx.Models.FullReport>>(Report) : null;
}
