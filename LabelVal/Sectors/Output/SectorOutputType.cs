using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SQLite;

namespace LabelVal.Sectors.Output;

/// <summary>
/// Specifies the output format for sector data.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
[StoreAsText]
public enum SectorOutputType
{
    /// <summary>
    /// Delimited text format (e.g., CSV).
    /// </summary>
    Delimited,
    /// <summary>
    /// Microsoft Excel format.
    /// </summary>
    //Excel,
    /// <summary>
    /// JSON format.
    /// </summary>
    JSON
}