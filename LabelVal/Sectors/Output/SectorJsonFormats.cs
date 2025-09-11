using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SQLite;

namespace LabelVal.Sectors.Output;

/// <summary>
/// Specifies the content detail level for JSON output.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
[StoreAsText]
public enum SectorJsonFormats
{
    /// <summary>
    /// Include only the parameters.
    /// </summary>
    ParamertersOnly,
    /// <summary>
    /// Include only the reports.
    /// </summary>
    ReportsOnly,
    /// <summary>
    /// Include all available data.
    /// </summary>
    Full
}