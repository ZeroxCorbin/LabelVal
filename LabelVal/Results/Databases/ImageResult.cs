using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.ImageRolls.ViewModels;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using V275_REST_lib.Models;

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

    [SQLite.Ignore] public ImageEntry Source => JsonConvert.DeserializeObject<ImageEntry>(SourceImage);
    [SQLite.Ignore] public ImageEntry Stored => JsonConvert.DeserializeObject<ImageEntry>(StoredImage);
}
public class V275Result : ImageResult
{
    [SQLite.Ignore] public Job _Job => JsonConvert.DeserializeObject<Job>(Template);
    [SQLite.Ignore] public Report _Report => JsonConvert.DeserializeObject<Report>(Report);
}
public class V5Result : ImageResult
{
    [SQLite.Ignore] public JObject _Config => JsonConvert.DeserializeObject<JObject>(Template);
    [SQLite.Ignore] public JObject _Report => JsonConvert.DeserializeObject<JObject>(Report);
}

public class L95xxResult : ImageResult { }
