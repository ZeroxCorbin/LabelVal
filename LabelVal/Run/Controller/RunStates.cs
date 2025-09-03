using Newtonsoft.Json.Converters;
using SQLite;
using System.Text.Json.Serialization;

namespace LabelVal.Run;
[JsonConverter(typeof(StringEnumConverter))]
[StoreAsText]
public enum RunStates
{
    Idle,
    Running,
    Paused,
    Stopped,
    Complete,
    Error
}
