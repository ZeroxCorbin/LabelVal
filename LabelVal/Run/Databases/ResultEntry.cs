using LabelVal.Results.Databases;
using LabelVal.Sectors.Interfaces;
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

public class ResultEntry
{
    [Ignore] public V275Result V275Result { get; set; }
    [Ignore] public V5Result V5Result { get; set; }
    [Ignore] public L95xxResult L95xxResult { get; set; }

    [PrimaryKey, AutoIncrement] public int ID { get; set; }

    public DeviceTypes DeviceType { get; set; }
    public ImageResultTypes ResultType { get; set; }

    public string RunUID { get; set; }
    public string ImageRollUID { get; set; }
    public string SourceImageUID { get; set; }

    public int Order { get; set; }

    //public string ImageRollName { get; set; }
    //public StandardsTypes GradingStandard { get; set; }
    //public Gs1TableNames Gs1TableName { get; set; }
    //public double TargetDPI { get; set; }

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
}
