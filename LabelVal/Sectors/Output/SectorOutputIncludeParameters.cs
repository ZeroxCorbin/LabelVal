using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SQLite;

namespace LabelVal.Sectors.Output;

/// <summary>
/// Specifies which parameters to include in the output.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
[StoreAsText]
public enum SectorOutputIncludeParameters
{
    /// <summary>
    /// Include only relevant parameters.
    /// </summary>
    Relevant,
    /// <summary>
    /// Include all parameters.
    /// </summary>
    All,
    /// <summary>
    /// Include only user-focused parameters.
    /// </summary>
    Focused
}