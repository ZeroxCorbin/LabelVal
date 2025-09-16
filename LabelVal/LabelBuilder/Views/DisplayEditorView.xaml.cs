using LabelVal.LabelBuilder.ViewModels;
using MaterialDesignThemes.Wpf;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using WPF.JoshSmith.Controls;


namespace LabelVal.LabelBuilder.Views
{
    /// <summary>
    /// Interaction logic for DisplayEditorView.xaml
    /// </summary>
    public partial class DisplayEditorView : UserControl
    {
        private UIElement elementForContextMenu;

        public DisplayEditorView()
        {
            InitializeComponent();
        }

        private void btnShowSettings_Click(object sender, RoutedEventArgs e) => drwSettings.IsTopDrawerOpen = true;

        private void btnAckMsg_Click(object sender, RoutedEventArgs e) => ((DisplayEditorViewModel)DataContext).StatusMessage = null;

        private void ContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            if (sender is ContextMenu menu)
                if (menu.PlacementTarget is Image img)
                {
                    var drg = GetDragCanvas(itcCanvas);

                    if (drg != null)
                    {
                        if (drg.ElementBeingDragged != null)
                            this.elementForContextMenu = drg.ElementBeingDragged;
                        else
                            this.elementForContextMenu = drg.FindCanvasChild(img as DependencyObject);

                    }
                }
        }

        private void mniBringtoFront_Click(object sender, RoutedEventArgs e)
        {
            if (elementForContextMenu == null) return;

            var pnl = GetDragCanvas(itcCanvas);
            if (pnl != null)
                if (e.Source is MenuItem menu)
                    pnl.BringToFront(elementForContextMenu);
        }

        private void mniSendToBack_Click(object sender, RoutedEventArgs e)
        {
            if (elementForContextMenu == null) return;

            var pnl = GetDragCanvas(itcCanvas);
            if (pnl != null)
                if (e.Source is MenuItem menu)
                    pnl.SendToBack(elementForContextMenu);
        }

        private void mniScaleUp_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is MenuItem menu)
                if (menu.DataContext is DisplayEditorViewModel.BarcodeEntity barcode)
                {
                    if (barcode.Position.Scale + 0.1 >= 5.0)
                    {
                        barcode.Position.Scale = 5.0;
                        return;
                    }


                    barcode.Position.Scale += 0.1;
                }
        }

        private void mniScaleDown_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is MenuItem menu)
                if (menu.DataContext is DisplayEditorViewModel.BarcodeEntity barcode)
                {
                    if (barcode.Position.Scale - 0.1 <= 0.1)
                    {
                        barcode.Position.Scale = 0.1;
                        return;
                    }

                    barcode.Position.Scale -= 0.1;
                }
        }

        private void mniScaleReset_Click(object sender, RoutedEventArgs e)
        {
            if (e.Source is MenuItem menu)
            {
                if (menu.DataContext is DisplayEditorViewModel.BarcodeEntity barcode)
                {
                    barcode.Position.Scale = 1.0;

                }
            }
        }

        private DragCanvas GetDragCanvas(DependencyObject itemsControl)
        {
            ItemsPresenter itemsPresenter = GetVisualChild<ItemsPresenter>(itemsControl);
            DragCanvas itemsPanel = VisualTreeHelper.GetChild(itemsPresenter, 0) as DragCanvas;
            return itemsPanel;
        }


        private static T GetVisual<T>(DependencyObject root, bool forward = false) where T : Visual
        {
            if (forward)
            {
                return GetVisualChild<T>(root) as T;
            }
            else
            {
                var res = GetVisualChild<T>(root) as T;
                if (res == null)
                {
                    Visual v = (Visual)VisualTreeHelper.GetParent(root);
                    if (v is not T parent)
                        parent = GetVisual<T>(v);

                    return parent;
                }
                else
                    return res;
            }
        }

        private static T GetVisualChild<T>(DependencyObject parent) where T : Visual
        {
            T child = default(T);

            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }


        private static T GetVisualParent<T>(DependencyObject child) where T : Visual
        {
            T parent = default(T);

            Visual v = (Visual)VisualTreeHelper.GetParent(child);
            parent = v as T;
            if (parent == null)
            {
                parent = GetVisualParent<T>(v);
            }

            return parent;
        }

        private void Image_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Image img)
            {
                var drg = GetDragCanvas(itcCanvas);

                if (drg != null)
                {
                    if (drg.ElementBeingDragged != null)
                        this.elementForContextMenu = drg.ElementBeingDragged;
                    else
                        this.elementForContextMenu = drg.FindCanvasChild(img as DependencyObject);

                    if (this.elementForContextMenu is ContentPresenter cp)

                        if (cp.DataContext is DisplayEditorViewModel.BarcodeEntity bar)
                            ((DisplayEditorViewModel)DataContext).SelectedBarcodeEntity = bar;
                }
            }
        }

        private void btnRenameDisplayOpen_Click(object sender, RoutedEventArgs e)
        {
            DrawerHost drawerHost = GetVisual<DrawerHost>((Button)sender);
            if (drawerHost != null)
            {
                var text = GetVisual<TextBox>((Button)sender);
                if (text != null)
                {
                    text.Text = ((DisplayEditorViewModel.DisplayEntity)text.DataContext).Name;
                    drawerHost.IsLeftDrawerOpen = true;
                }
            }
        }

        private void btnRenameDisplayClose_Click(object sender, RoutedEventArgs e)
        {
            DrawerHost drawerHost = GetVisual<DrawerHost>((Button)sender);
            if (drawerHost != null)
                drawerHost.IsLeftDrawerOpen = false;
        }

        private void btnRenameDisplayAccept_Click(object sender, RoutedEventArgs e)
        {
            DrawerHost drawerHost = GetVisual<DrawerHost>((Button)sender);
            if (drawerHost != null)
            {
                var text = GetVisual<TextBox>((Button)sender);
                if (text != null)
                {
                    ((DisplayEditorViewModel.DisplayEntity)text.DataContext).Rename(text.Text);
                    drawerHost.IsLeftDrawerOpen = false;
                }
            }
        }
    }
}
