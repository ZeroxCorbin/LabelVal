using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SQLite;

namespace LabelVal.Sectors.Output;

/// <summary>
/// Specifies the string quoting behavior for delimited output.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
[StoreAsText]
public enum SectorOutputStringQuote
{
    /// <summary>
    /// Do not quote strings.
    /// </summary>
    None,
    /// <summary>
    /// Quote all strings.
    /// </summary>
    All,
    /// <summary>
    /// Quote only strings containing the delimiter character.
    /// </summary>
    Delimiter
}