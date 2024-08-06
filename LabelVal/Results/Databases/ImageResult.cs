using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using V275_REST_lib.Models;
using V5_REST_Lib.Models;

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
    [SQLite.Ignore] public Job _Job => !string.IsNullOrEmpty(Template) ? JsonConvert.DeserializeObject<Job>(Template) : null;
    [SQLite.Ignore] public Report _Report => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<Report>(Report): null;
}
public class V5Result : ImageResult
{
    [SQLite.Ignore] public Config _Config => !string.IsNullOrEmpty(Template) ? JsonConvert.DeserializeObject<Config>(Template) : null;
    [SQLite.Ignore] public ResultsAlt _Report => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<ResultsAlt>(Report) : null;
}

public class L95xxResult : ImageResult 
{ 
    [SQLite.Ignore] public List<ViewModels.ImageResultEntry.L95xxReport> _Report => !string.IsNullOrEmpty(Report) ? JsonConvert.DeserializeObject<List<ViewModels.ImageResultEntry.L95xxReport>>(Report) : null;
}
