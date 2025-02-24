using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;
using System.Globalization;

namespace LabelVal.Run.ViewModels;
public partial class MainWindow : ObservableRecipient
{
    public static string Version => App.Version;

    public RunDatabases RunDatabases { get; set; } = new();
    public RunResults RunResults { get; set; } = new();

    public List<string> Languages { get; } = ["English", "Español"];
    [ObservableProperty] private string selectedLanguage = App.Settings.GetValue(nameof(SelectedLanguage), "English", true);
    partial void OnSelectedLanguageChanged(string value)
    {
        Localization.TranslationSource.Instance.CurrentCulture = CultureInfo.GetCultureInfo(Localization.Culture.GetCulture(value));
        App.Settings.SetValue(nameof(SelectedLanguage), value);
    }

    public MainWindow() => IsActive = true;

}
