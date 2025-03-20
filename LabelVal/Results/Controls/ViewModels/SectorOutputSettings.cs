using CommunityToolkit.Mvvm.ComponentModel;

namespace LabelVal.Results.Controls.ViewModels;

public enum SectorOutputType
{
    Delimited,
    Excel,
    JSON
}

public enum SectorOutputDelimiter
{
    Comma = ',',
    Semicolon = ';',
    Tab = '\t',
    Pipe = '|'
}

public enum SectorOutputStringQuote
{
    None,
    All,
    Delimiter
}

public enum SectorOutputIncludeParameters
{
    Relevant,
    All,
    //Selected
}

public partial class SectorOutputSettings : ObservableObject
{
    public static string ValueName(string name) => typeof(SectorOutputSettings).ToString() + name;

public static SectorOutputType CurrentOutputType => App.Settings.GetValue(ValueName(nameof(OutputType)), SectorOutputType.Delimited, true);

    [ObservableProperty] SectorOutputType outputType = App.Settings.GetValue(ValueName(nameof(OutputType)), SectorOutputType.Delimited, true);
    partial void OnOutputTypeChanged(SectorOutputType value) => App.Settings.SetValue(ValueName(nameof(OutputType)), value);

    public static SectorOutputDelimiter CurrentDelimiter => App.Settings.GetValue(ValueName(nameof(Delimiter)), SectorOutputDelimiter.Comma, true);

    [ObservableProperty] SectorOutputDelimiter delimiter = App.Settings.GetValue(ValueName(nameof(Delimiter)), SectorOutputDelimiter.Comma, true);
    partial void OnDelimiterChanged(SectorOutputDelimiter value) => App.Settings.SetValue(ValueName(nameof(Delimiter)), value);

    public static SectorOutputStringQuote CurrentStringQuote => App.Settings.GetValue(ValueName(nameof(StringQuote)), SectorOutputStringQuote.None, true);

    [ObservableProperty] SectorOutputStringQuote stringQuote = App.Settings.GetValue(ValueName(nameof(StringQuote)), SectorOutputStringQuote.None, true);
    partial void OnStringQuoteChanged(SectorOutputStringQuote value) => App.Settings.SetValue(ValueName(nameof(StringQuote)), value);

    public static SectorOutputIncludeParameters CurrentIncludeParameters => App.Settings.GetValue(ValueName(nameof(IncludeParameters)), SectorOutputIncludeParameters.Relevant, true);

    [ObservableProperty] SectorOutputIncludeParameters includeParameters = App.Settings.GetValue(ValueName(nameof(IncludeParameters)), SectorOutputIncludeParameters.Relevant, true);
    partial void OnIncludeParametersChanged(SectorOutputIncludeParameters value) => App.Settings.SetValue(ValueName(nameof(IncludeParameters)), value);
}
