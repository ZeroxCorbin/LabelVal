using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Sectors.Classes;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace LabelVal.Main.ViewModels;

public class DPIChangedMessage : ValueChangedMessage<DpiScale> { public DPIChangedMessage(DpiScale value) : base(value) { } }

public partial class MainWindow : ObservableRecipient
{
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    [ObservableProperty]
    private string _applicationTitle = $"Label Validator : {GetAssemblyVersion()} : CONFIDENTIAL B (Internal Use Only)";

    public LoggingStatusBarVm LoggingStatusBarVm { get; } = new LoggingStatusBarVm();

    [ObservableProperty] private DPIChangedMessage dPIChangedMessage;
    partial void OnDPIChangedMessageChanged(DPIChangedMessage value) => _ = WeakReferenceMessenger.Default.Send(value);

    public ObservableCollection<HamburgerMenuItem> MenuItems { get; }
    [ObservableProperty] private HamburgerMenuItem selectedMenuItem;

    public void SetDeafultMenuItem() => SelectedMenuItem = MenuItems[0];

    public V275.ViewModels.V275Manager V275Manager { get; }
    public V275.ViewModels.NodeDetails NodeDetails { get; }

    public Printer.ViewModels.Printer Printer { get; }
    public Printer.ViewModels.PrinterDetails PrinterDetails { get; }

    public ImageRolls.ViewModels.ImageRolls ImageRolls { get; }
    public Results.ViewModels.ImageResultsManager ImageResults { get; }

    public V5.ViewModels.ScannerManager ScannerManager { get; }
    public V5.ViewModels.ScannerDetails ScannerDetails { get; }

    public Run.ViewModels.RunManager RunManager { get; }

    public L95.ViewModels.VerifierManager VerifierManager { get; }

    public Results.ViewModels.ImageResultsDatabases ImageResultsDatabases { get; }

    public List<string> Languages { get; } = ["English", "Español"];
    [ObservableProperty] private string selectedLanguage = App.Settings.GetValue(nameof(SelectedLanguage), "English", true);
    partial void OnSelectedLanguageChanged(string value)
    {
        Localization.TranslationSource.Instance.CurrentCulture = CultureInfo.GetCultureInfo(Localization.Culture.GetCulture(value));
        App.Settings.SetValue(nameof(SelectedLanguage), value);
    }

    public SectorOutputSettings SectorOutputSettings { get; } = new SectorOutputSettings();

    public MainWindow()
    {
        Localization.TranslationSource.Instance.CurrentCulture = CultureInfo.GetCultureInfo(Localization.Culture.GetCulture(selectedLanguage));

        IsActive = true;

        //Should be loaded first to allow the property changed events to be received
        NodeDetails = new V275.ViewModels.NodeDetails();
        PrinterDetails = new Printer.ViewModels.PrinterDetails();
        ScannerDetails = new V5.ViewModels.ScannerDetails();

        Printer = new Printer.ViewModels.Printer();
        V275Manager = new V275.ViewModels.V275Manager();
        ScannerManager = new V5.ViewModels.ScannerManager();
        VerifierManager = new L95.ViewModels.VerifierManager();

        ImageRolls = new ImageRolls.ViewModels.ImageRolls();
        ImageResultsDatabases = new Results.ViewModels.ImageResultsDatabases();
        ImageResults = new Results.ViewModels.ImageResultsManager();

        RunManager = new Run.ViewModels.RunManager(ImageResults);

        MenuItems =
        [
            new HamburgerMenuItem { Label = "ImageRolls", Content = ImageRolls }, 
            new HamburgerMenuItem { Label = "Results", Content = ImageResultsDatabases },
            new HamburgerMenuItem { Label = "Printer", Content = Printer, IsNotSelectable = true },
            new HamburgerMenuItem { Label = "Run", Content = RunManager },


            new HamburgerMenuItem { Label = "V275", Content = V275Manager },
            new HamburgerMenuItem { Label = "V5", Content = ScannerManager },
            new HamburgerMenuItem { Label = "L95", Content = VerifierManager },
        ];

        SelectedMenuItem = MenuItems[0];
    }
    private static string GetAssemblyVersion() => System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
        ?? string.Empty;
}
