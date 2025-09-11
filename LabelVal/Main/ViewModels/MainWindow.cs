using BarcodeBuilder.lib.Wpf.ViewModels;
using BarcodeBuilder.lib.Wpf.ViewModels.Browser;
using BarcodeVerification.lib.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.Sectors.Classes;
using LabelVal.Sectors.Output;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;

namespace LabelVal.Main.ViewModels;

/// <summary>
/// Message for broadcasting DPI scale changes.
/// </summary>
public class DPIChangedMessage : ValueChangedMessage<DpiScale>
{
    public DPIChangedMessage(DpiScale value) : base(value) { }
}

/// <summary>
/// Main window ViewModel for Label Validator application.
/// Manages global state, navigation, and sub-viewmodels.
/// </summary>
public partial class MainWindow : ObservableRecipient
{
    #region Application Settings and Status

    /// <summary>
    /// Singleton instance for global application settings.
    /// </summary>
    public GlobalAppSettings AppSettings => GlobalAppSettings.Instance;

    /// <summary>
    /// Application title displayed in the window.
    /// </summary>
    [ObservableProperty]
    private string _applicationTitle = $"Label Validator : {GetAssemblyVersion()} : For Internal Use Only! (Confidential B)";

    /// <summary>
    /// Status bar ViewModel for logging messages.
    /// </summary>
    public LoggingStatusBarVm LoggingStatusBarVm { get; } = new LoggingStatusBarVm();

    #endregion

    #region DPI and Messaging

    /// <summary>
    /// Message for DPI changes.
    /// </summary>
    [ObservableProperty]
    private DPIChangedMessage dPIChangedMessage;

    /// <summary>
    /// Broadcasts DPI change messages to listeners.
    /// </summary>
    partial void OnDPIChangedMessageChanged(DPIChangedMessage value) => _ = WeakReferenceMessenger.Default.Send(value);

    #endregion

    #region Navigation (Hamburger Menu)

    /// <summary>
    /// Collection of menu items for navigation.
    /// </summary>
    public ObservableCollection<HamburgerMenuItem> MenuItems { get; }

    /// <summary>
    /// Currently selected menu item.
    /// </summary>
    [ObservableProperty]
    private HamburgerMenuItem selectedMenuItem;

    /// <summary>
    /// Sets the default menu item (first item).
    /// </summary>
    public void SetDeafultMenuItem() => SelectedMenuItem = MenuItems[0];

    #endregion

    #region Sub-ViewModels

    // V275 Section
    public V275.ViewModels.V275Manager V275Manager { get; }
    public V275.ViewModels.NodeDetails NodeDetails { get; }

    // Label Builder Section
    public LabelBuilder.ViewModels.LabelBuilderViewModel LabelBuilderViewModel { get; }

    // Printer Section
    public Printer.ViewModels.Printer Printer { get; }
    public Printer.ViewModels.PrinterDetails PrinterDetails { get; }

    // Image Rolls Section
    public ImageRolls.ViewModels.ImageRollsManager ImageRolls { get; }
    public Results.ViewModels.ResultsManagerViewModel ResultsManagerView { get; }

    // Scanner Section
    public V5.ViewModels.ScannerManager ScannerManager { get; }
    public V5.ViewModels.ScannerDetails ScannerDetails { get; }

    // Run Section
    public Run.ViewModels.RunManager RunManager { get; }

    // Verifier Section
    public L95.ViewModels.VerifierManager VerifierManager { get; }

    // Results Section
    public Results.ViewModels.ResultsDatabasesViewModel ResultssDatabases { get; }

    // Enum Browser
    public EnumBrowserViewModel EnumBrowserViewModel { get; }

    #endregion

    #region Parameters and Localization

    /// <summary>
    /// List of selected parameters for the application.
    /// </summary>
    [ObservableProperty]
    private List<Parameters> selectedParameters = App.Settings.GetValue(nameof(SelectedParameters), new List<Parameters>(), true);

    /// <summary>
    /// Updates settings when selected parameters change.
    /// </summary>
    partial void OnSelectedParametersChanged(List<Parameters> value) => App.Settings.SetValue(nameof(SelectedParameters), value);

    /// <summary>
    /// Supported languages for localization.
    /// </summary>
    public List<string> Languages { get; } = ["English", "Español"];

    /// <summary>
    /// Currently selected language.
    /// </summary>
    [ObservableProperty]
    private string selectedLanguage = App.Settings.GetValue(nameof(SelectedLanguage), "English", true);

    /// <summary>
    /// Updates localization and settings when language changes.
    /// </summary>
    partial void OnSelectedLanguageChanged(string value)
    {
        Localization.TranslationSource.Instance.CurrentCulture = CultureInfo.GetCultureInfo(Localization.Culture.GetCulture(value));
        App.Settings.SetValue(nameof(SelectedLanguage), value);
    }

    /// <summary>
    /// Gets selected parameters from settings.
    /// </summary>
    private List<Parameters> _selectedParameters => App.Settings.GetValue("SelectedParameters", new List<Parameters>(), true);

    #endregion

    #region Sector Output

    /// <summary>
    /// Output settings for sector reports.
    /// </summary>
    public SectorOutputSettings SectorOutputSettings { get; } = new SectorOutputSettings();

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes the main window ViewModel and all sub-viewmodels.
    /// </summary>
    public MainWindow()
    {
        // Set initial culture for localization
        Localization.TranslationSource.Instance.CurrentCulture = CultureInfo.GetCultureInfo(Localization.Culture.GetCulture(selectedLanguage));

        IsActive = true;

        // Initialize details first for property change event handling
        NodeDetails = new V275.ViewModels.NodeDetails();
        PrinterDetails = new Printer.ViewModels.PrinterDetails();
        ScannerDetails = new V5.ViewModels.ScannerDetails();

        // Enum browser setup
        EnumBrowserViewModel = new EnumBrowserViewModel(_selectedParameters);
        EnumBrowserViewModel.SelectedParametersChanged += EnumBrowserViewModel_SelectedParametersChanged;

        // Initialize main modules
        Printer = new Printer.ViewModels.Printer();
        V275Manager = new V275.ViewModels.V275Manager();
        ScannerManager = new V5.ViewModels.ScannerManager();
        VerifierManager = new L95.ViewModels.VerifierManager();

        ImageRolls = new ImageRolls.ViewModels.ImageRollsManager();
        ResultssDatabases = new Results.ViewModels.ResultsDatabasesViewModel();
        ResultsManagerView = new Results.ViewModels.ResultsManagerViewModel();

        RunManager = new Run.ViewModels.RunManager(ResultsManagerView);

        LabelBuilderViewModel = new LabelBuilder.ViewModels.LabelBuilderViewModel();

        // Setup navigation menu items
        MenuItems =
        [
            new HamburgerMenuItem { Label = "ImageRolls", Content = ImageRolls },
            new HamburgerMenuItem { Label = "Results", Content = ResultssDatabases },
            new HamburgerMenuItem { Label = "Printer", Content = Printer, IsNotSelectable = true },
            //new HamburgerMenuItem { Label = "Run", Content = RunManager },
            new HamburgerMenuItem { Label = "Label", Content = LabelBuilderViewModel },
            new HamburgerMenuItem { Label = "V275", Content = V275Manager },
            new HamburgerMenuItem { Label = "V5", Content = ScannerManager },
            new HamburgerMenuItem { Label = "L95", Content = VerifierManager },
        ];

        SelectedMenuItem = MenuItems[0];
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// Handles changes to selected parameters from the EnumBrowserViewModel.
    /// </summary>
    private void EnumBrowserViewModel_SelectedParametersChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        => SelectedParameters = (List<Parameters>)sender;

    #endregion

    #region Utility

    /// <summary>
    /// Gets the current assembly version for display.
    /// </summary>
    private static string GetAssemblyVersion() =>
        System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? string.Empty;

    #endregion
}