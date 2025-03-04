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
    L95xx
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

    [ObservableProperty][property: Ignore] private V275Result v275Result;
    partial void OnV275ResultChanged(V275Result value) { _results = JsonConvert.SerializeObject(value); DeviceType = DeviceTypes.V275; }

    [ObservableProperty][property: Ignore] private V5Result v5Result;
    partial void OnV5ResultChanged(V5Result value) { _results = JsonConvert.SerializeObject(value); DeviceType = DeviceTypes.V5; }

    [ObservableProperty][property: Ignore] private L95xxResult l95xxResult;
    partial void OnL95xxResultChanged(L95xxResult value) { _results = JsonConvert.SerializeObject(value); DeviceType = DeviceTypes.L95xx; }

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
                DeviceTypes.V275 => V275Result = JsonConvert.DeserializeObject<V275Result>(_results = value),
                DeviceTypes.V5 => V5Result = JsonConvert.DeserializeObject<V5Result>(_results = value),
                DeviceTypes.L95xx => L95xxResult = JsonConvert.DeserializeObject<L95xxResult>(_results = value),
                _ => _results = null
            };
        }
    }

    public ResultEntry(object results, ImageResultTypes type) 
    {
        ResultType = type;

        if (results is V275Result v275Result)
            V275Result = v275Result;
        
        else if (results is V5Result v5Result)
            V5Result = v5Result;
        
        else if (results is L95xxResult l95xxResult)
            L95xxResult = l95xxResult;
        
    }
    public ResultEntry() { }
}
