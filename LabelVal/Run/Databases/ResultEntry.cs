using CommunityToolkit.Mvvm.ComponentModel;
using LabelVal.Results.Databases;
using Newtonsoft.Json;
using SQLite;

namespace LabelVal.Run.Databases;

[StoreAsText]
public enum DeviceTypes
{
    V275,
    V5,
    L95
}

[StoreAsText]
public enum ImageResultTypes
{
    Stored,
    Current,
    Source
}

public partial class ResultEntry : ObservableObject
{
    [PrimaryKey, AutoIncrement] public int ID { get; set; }

    [ObservableProperty][property: Ignore] private Result v275Result;
    partial void OnV275ResultChanged(Result value) { _results = JsonConvert.SerializeObject(value); DeviceType = DeviceTypes.V275; }

    [ObservableProperty][property: Ignore] private Result v5Result;
    partial void OnV5ResultChanged(Result value) { _results = JsonConvert.SerializeObject(value); DeviceType = DeviceTypes.V5; }

    [ObservableProperty][property: Ignore] private Result l95Result;
    partial void OnL95ResultChanged(Result value) { _results = JsonConvert.SerializeObject(value); DeviceType = DeviceTypes.L95; }

    public DeviceTypes DeviceType { get; set; }
    public ImageResultTypes ResultType { get; set; }

    public string RunUID { get; set; }
    public string ImageRollUID { get; set; }
    public string SourceImageUID { get; set; }

    public int Order { get; set; }
    public int TotalLoops { get; set; }
    public int CompletedLoops { get; set; }

    private string _results;
    public string Results
    {
        get => _results;
        set
        {
            object value1 = DeviceType switch
            {
                DeviceTypes.V275 => V275Result = JsonConvert.DeserializeObject<Result>(_results = value),
                DeviceTypes.V5 => V5Result = JsonConvert.DeserializeObject<Result>(_results = value),
                DeviceTypes.L95 => L95Result = JsonConvert.DeserializeObject<Result>(_results = value),
                _ => _results = null
            };
        }
    }

    public ResultEntry(object results, ImageResultTypes type) 
    {
        ResultType = type;

        if (results is Result v275Result)
            V275Result = v275Result;
        else if (results is Result v5Result)
            V5Result = v5Result;
        else if (results is Result l95Result)
            L95Result = l95Result;
    }
    public ResultEntry() { }
}
