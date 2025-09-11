using CommunityToolkit.Mvvm.ComponentModel;

namespace LabelVal.Sectors.Output;

/// <summary>
/// Manages and persists user-defined settings for sector data output.
/// This class uses observable properties for data binding and persists changes to application settings.
/// </summary>
public partial class SectorOutputSettings : ObservableObject
{
    /// <summary>
    /// Generates a unique key for a setting value based on the property name.
    /// </summary>
    /// <param name="name">The name of the property.</param>
    /// <returns>A unique string key for application settings.</returns>
    public static string ValueName(string name) => typeof(SectorOutputSettings).ToString() + name;

    #region Output Type

    /// <summary>
    /// Gets the currently persisted output type setting.
    /// </summary>
    public static SectorOutputType CurrentOutputType => App.Settings.GetValue(ValueName(nameof(OutputType)), SectorOutputType.Delimited, true);

    [ObservableProperty]
    private SectorOutputType outputType = App.Settings.GetValue(ValueName(nameof(OutputType)), SectorOutputType.Delimited, true);

    /// <summary>
    /// Persists the OutputType value when it changes.
    /// </summary>
    partial void OnOutputTypeChanged(SectorOutputType value) => App.Settings.SetValue(ValueName(nameof(OutputType)), value);

    #endregion

    #region Delimiter

    /// <summary>
    /// Gets the currently persisted delimiter setting.
    /// </summary>
    public static SectorOutputDelimiter CurrentDelimiter => App.Settings.GetValue(ValueName(nameof(Delimiter)), SectorOutputDelimiter.Comma, true);

    [ObservableProperty]
    private SectorOutputDelimiter delimiter = App.Settings.GetValue(ValueName(nameof(Delimiter)), SectorOutputDelimiter.Comma, true);

    /// <summary>
    /// Persists the Delimiter value when it changes.
    /// </summary>
    partial void OnDelimiterChanged(SectorOutputDelimiter value) => App.Settings.SetValue(ValueName(nameof(Delimiter)), value);

    #endregion

    #region JSON Format

    /// <summary>
    /// Gets the currently persisted JSON format setting.
    /// </summary>
    public static SectorJsonFormats CurrentJsonFormat => App.Settings.GetValue(ValueName(nameof(JsonFormat)), SectorJsonFormats.Full, true);

    [ObservableProperty]
    private SectorJsonFormats jsonFormat = App.Settings.GetValue(ValueName(nameof(JsonFormat)), SectorJsonFormats.Full, true);

    /// <summary>
    /// Persists the JsonFormat value when it changes.
    /// </summary>
    partial void OnJsonFormatChanged(SectorJsonFormats value) => App.Settings.SetValue(ValueName(nameof(JsonFormat)), value);

    #endregion

    #region String Quote

    /// <summary>
    /// Gets the currently persisted string quote setting.
    /// </summary>
    public static SectorOutputStringQuote CurrentStringQuote => App.Settings.GetValue(ValueName(nameof(StringQuote)), SectorOutputStringQuote.None, true);

    [ObservableProperty]
    private SectorOutputStringQuote stringQuote = App.Settings.GetValue(ValueName(nameof(StringQuote)), SectorOutputStringQuote.None, true);

    /// <summary>
    /// Persists the StringQuote value when it changes.
    /// </summary>
    partial void OnStringQuoteChanged(SectorOutputStringQuote value) => App.Settings.SetValue(ValueName(nameof(StringQuote)), value);

    #endregion

    #region Include Parameters

    /// <summary>
    /// Gets the currently persisted parameter inclusion setting.
    /// </summary>
    public static SectorOutputIncludeParameters CurrentIncludeParameters => App.Settings.GetValue(ValueName(nameof(IncludeParameters)), SectorOutputIncludeParameters.Relevant, true);

    [ObservableProperty]
    private SectorOutputIncludeParameters includeParameters = App.Settings.GetValue(ValueName(nameof(IncludeParameters)), SectorOutputIncludeParameters.Relevant, true);

    /// <summary>
    /// Persists the IncludeParameters value when it changes.
    /// </summary>
    partial void OnIncludeParametersChanged(SectorOutputIncludeParameters value) => App.Settings.SetValue(ValueName(nameof(IncludeParameters)), value);

    #endregion
}