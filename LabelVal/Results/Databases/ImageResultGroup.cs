using Newtonsoft.Json;

namespace LabelVal.Results.Databases;

public class ImageResultGroup()
{
    [SQLite.Ignore] public V275Result V275Result { get; set; }
    [SQLite.Ignore] public V5Result V5Result { get; set; }
    [SQLite.Ignore] public L95xxResult L95xxResult { get; set; }

    public string RunUID { get; set; }
    public string ImageRollUID { get; set; }
    public string SourceImageUID { get; set; }

    public int LoopCount { get; set; }
    public int Order { get; set; }
    public int Loop { get; set; }

    public string V275Result_
    {
        get => V275Result != null ? JsonConvert.SerializeObject(V275Result) : null;
        set { if (value != null) V275Result = JsonConvert.DeserializeObject<V275Result>(value); else V275Result = null; }
    }
    public string V5Result_
    {
        get => V5Result != null ? JsonConvert.SerializeObject(V5Result) : null;
        set { if (value != null) V5Result = JsonConvert.DeserializeObject<V5Result>(value); else V5Result = null; }
    }
    public string L95xxResult_
    {
        get => L95xxResult != null ? JsonConvert.SerializeObject(L95xxResult) : null;
        set { if (value != null) L95xxResult = JsonConvert.DeserializeObject<L95xxResult>(value); else L95xxResult = null; }
    }
}

public class StoredImageResultGroup : ImageResultGroup { }

public class CurrentImageResultGroup : ImageResultGroup { }