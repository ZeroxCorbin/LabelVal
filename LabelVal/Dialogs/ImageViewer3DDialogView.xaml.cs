using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.Dialogs;

public partial class ImageViewer3DDialogView : MahApps.Metro.Controls.Dialogs.CustomDialog
{
    public ImageViewer3DDialogView() => InitializeComponent();

    private void CustomDialog_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
            Close();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private async void Close()
    {
        await MahApps.Metro.Controls.Dialogs.DialogCoordinator.Instance.HideMetroDialogAsync(this.DataContext, this);
        MahApps.Metro.Controls.Dialogs.DialogParticipation.SetRegister(this, null);
        
        this.DataContext = null;
    }

    private void CustomDialog_Loaded(object sender, RoutedEventArgs e)
    {

    }

}
