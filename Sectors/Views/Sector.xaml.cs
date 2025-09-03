using LabelVal.Results.Views;
using LabelVal.Sectors.Interfaces;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace LabelVal.Sectors.Views
{
    public partial class Sector : UserControl
    {
        public static readonly DependencyProperty HideErrorsWarningsProperty =
                                DependencyProperty.Register(
                                nameof(HideErrorsWarnings),
                                typeof(bool),
                                typeof(Sector),
                                new FrameworkPropertyMetadata(App.Settings.GetValue<bool>(nameof(HideErrorsWarnings)), FrameworkPropertyMetadataOptions.AffectsMeasure));

        public bool HideErrorsWarnings
        {
            get => (bool)GetValue(HideErrorsWarningsProperty);
            set => SetValue(HideErrorsWarningsProperty, value);
        }

        private ISector ThisSector { get; set; }
        public string GroupName { get; private set; }
        private Results.ViewModels.IImageResultDeviceEntry ImageResultEntry { get; set; }
        private readonly PopupGS1DecodeText popGS1DecodeText = new();

        public Sector()
        {
            InitializeComponent();
            App.Settings.PropertyChanged += Settings_PropertyChanged;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(HideErrorsWarnings))
            {
                HideErrorsWarnings = App.Settings.GetValue<bool>(nameof(HideErrorsWarnings));
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            ThisSector = (ISector)DataContext;
            GetSectorDetails();
        }

        private void GetSectorDetails()
        {
            var itemsControl = Utilities.VisualTreeHelp.GetVisualParent<ItemsControl>(this);
            if (itemsControl != null)
            {
                if (itemsControl.Tag is string tag)
                {
                    GroupName = tag;
                }
                ImageResultEntry = (Results.ViewModels.IImageResultDeviceEntry)itemsControl.DataContext;
            }
        }

        private void btnGS1DecodeText_Click(object sender, RoutedEventArgs e)
        {
            popGS1DecodeText.DataContext = DataContext;
            popGS1DecodeText.Popup.PlacementTarget = gs1AiTextPopAnchor;
            popGS1DecodeText.Popup.IsOpen = true;
        }

        private void btnOverallGrade_Click(object sender, RoutedEventArgs e)
        {
            if (ImageResultEntry == null || string.IsNullOrEmpty(GroupName))
                return;

            var listType = GroupName.EndsWith("Stored") ? "Stored" : "Current";

            if (listType == "Stored")
            {
                ImageResultEntry.FocusedStoredSector = ThisSector;
            }
            else // Current
            {
                ImageResultEntry.FocusedCurrentSector = ThisSector;
            }
        }

        private void UserControl_MouseEnter(object sender, MouseEventArgs e)
        {
            if (ThisSector != null)
            {
                ThisSector.IsMouseOver = true;
                if (ImageResultEntry != null)
                {
                    if (GroupName.EndsWith("Stored"))
                    {
                        ImageResultEntry.RefreshStoredOverlay();
                    }
                    else
                    {
                        ImageResultEntry.RefreshCurrentOverlay();
                    }
                }
            }
        }

        private void UserControl_MouseLeave(object sender, MouseEventArgs e)
        {
            if (ThisSector != null)
            {
                ThisSector.IsMouseOver = false;
                if (ImageResultEntry != null)
                {
                    if (GroupName.EndsWith("Stored"))
                    {
                        ImageResultEntry.RefreshStoredOverlay();
                    }
                    else
                    {
                        ImageResultEntry.RefreshCurrentOverlay();
                    }
                }
            }
        }
    }
}