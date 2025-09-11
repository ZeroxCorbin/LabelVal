using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using SQLite;

namespace LabelVal.Sectors.Output;

/// <summary>
/// Specifies the delimiter character for delimited output.
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
[StoreAsText]
public enum SectorOutputDelimiter
{
    /// <summary>
    /// Comma (,)
    /// </summary>
    Comma = ',',
    /// <summary>
    /// Semicolon (;)
    /// </summary>
    Semicolon = ';',
    /// <summary>
    /// Tab character.
    /// </summary>
    Tab = '\t',
    /// <summary>
    /// Pipe character (|).
    /// </summary>
    Pipe = '|'
}