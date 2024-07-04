using Newtonsoft.Json;

namespace LabelVal.Results.Databases;

public class ImageResultGroup()
{
    [SQLite.Ignore] public V275Result V275Result { get; set; }
    [SQLite.Ignore] public V5Result V5Result { get; set; }
    [SQLite.Ignore] public L95xxResult L95xxResult { get; set; }

    [SQLite.PrimaryKey] public string RunUID { get; set; }
    public string ImageRollUID { get; set; }
    public string SourceImageUID { get; set; }

    public string V275Result_
    {
        get => JsonConvert.SerializeObject(V275Result);
        set => V275Result = JsonConvert.DeserializeObject<V275Result>(value);
    }
    public string V5Result_
    {
        get => JsonConvert.SerializeObject(V5Result);
        set => V5Result = JsonConvert.DeserializeObject<V5Result>(value);
    }
    public string L95xxResult_
    {
        get => JsonConvert.SerializeObject(L95xxResult);
        set => L95xxResult = JsonConvert.DeserializeObject<L95xxResult>(value);
    }
}

public class StoredImageResultGroup : ImageResultGroup { }

public class CurrentImageResultGroup : ImageResultGroup { }