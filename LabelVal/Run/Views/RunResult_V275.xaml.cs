using LabelVal.Results.Databases;
using LabelVal.Results.Views;
using LabelVal.Sectors.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LabelVal.Run.Views;
/// <summary>
/// Interaction logic for RunResult_V275.xaml
/// </summary>
public partial class RunResult_V275 : UserControl
{
    public RunResult_V275()
    {
        InitializeComponent();
    }

    private void btnCloseDetails_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            ((ViewModels.RunResult)DataContext).V275FocusedStoredSector = null;
            ((ViewModels.RunResult)DataContext).V275FocusedCurrentSector = null;
            ((ViewModels.RunResult)DataContext).V5FocusedStoredSector = null;
            ((ViewModels.RunResult)DataContext).V5FocusedCurrentSector = null;
            //((ViewModels.RunResult)DataContext).L95FocusedStoredSector = null;
            //((ViewModels.RunResult)DataContext).L95FocusedCurrentSector = null;
        }
        else
        {
            switch ((string)((Button)sender).Tag)
            {
                case "v275Stored":
                    ((ViewModels.RunResult)DataContext).V275FocusedStoredSector = null;
                    break;
                case "v275Current":
                    ((ViewModels.RunResult)DataContext).V275FocusedCurrentSector = null;
                    ((ViewModels.RunResult)DataContext).V275FocusedStoredSector = null;
                    break;
            }
        }
    }

    private void V275StoredSectors_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            if (((ViewModels.RunResult)DataContext).StoredImageResultGroup.V275Result != null)
            {

            }
        }
        else
        {
            PopupSectorsDetails pop = new()
            {
                DataContext = ((ViewModels.RunResult)DataContext).V275StoredSectors
            };

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
        }
    }
    private void V275CurrentSector_Click(object sender, RoutedEventArgs e)
    {
        if (Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
        {
            //PopupJSONViewer pop = new();
            //pop.Viewer1.JSON = ((ViewModels.RunResult)DataContext).CurrentImageResultGroup.V275Result.Template;
            //pop.Viewer1.Title = "Template";
            //pop.Viewer2.JSON = ((ViewModels.RunResult)DataContext).CurrentImageResultGroup.V275Result.Report;
            //pop.Viewer2.Title = "Report";

            //pop.Popup.PlacementTarget = (Button)sender;
            //pop.Popup.IsOpen = true;
        }
        else
        {
            PopupSectorsDetails pop = new()
            {
                DataContext = ((ViewModels.RunResult)DataContext).V275CurrentSectors
            };

            pop.Popup.PlacementTarget = (Button)sender;
            pop.Popup.IsOpen = true;
        }
    }

    private void ScrollV275StoredSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollV275CurrentSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }
    private void ScrollV275CurrentSectors_ScrollChanged(object sender, ScrollChangedEventArgs e)
    {
        if (e.VerticalChange != 0)
            ScrollV275StoredSectors.ScrollToVerticalOffset(e.VerticalOffset);
    }


    private void btnSaveImage_Click(object sender, RoutedEventArgs e)
    {
        var sect = Utilities.VisualTreeHelp.GetVisualParent<Sector>((Button)sender);

        if (sect != null)
        {
            string path;
            if ((path = Utilities.FileUtilities.SaveFileDialog("plot", "PNG|*.png", "Save sector details.")) != "")
            {
                try
                {
                    SaveToPng(sect, path);
                }
                catch { }
            }
        }
    }
    private void btnCopyImage_Click(object sender, RoutedEventArgs e)
    {
        var sect = Utilities.VisualTreeHelp.GetVisualParent<Sector>((Button)sender);

        if (sect != null)
            CopyToClipboard(sect);

    }
    public void SaveToPng(FrameworkElement visual, string fileName)
    {
        PngBitmapEncoder encoder = new();
        EncodeVisual(visual, encoder);

        using var stream = System.IO.File.Create(fileName);
        encoder.Save(stream);
    }
    public void CopyToClipboard(FrameworkElement visual)
    {
        PngBitmapEncoder encoder = new();
        EncodeVisual(visual, encoder);

        using System.IO.MemoryStream stream = new();
        encoder.Save(stream);
        stream.Seek(0, System.IO.SeekOrigin.Begin);
        BitmapImage bitmapImage = new();
        bitmapImage.BeginInit();
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.StreamSource = stream;
        bitmapImage.EndInit();
        Clipboard.SetImage(bitmapImage);
    }
    private static void EncodeVisual(FrameworkElement visual, BitmapEncoder encoder)
    {
        RenderTargetBitmap bitmap = new((int)visual.ActualWidth, (int)visual.ActualHeight, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var frame = BitmapFrame.Create(bitmap);
        encoder.Frames.Add(frame);
    }

}
