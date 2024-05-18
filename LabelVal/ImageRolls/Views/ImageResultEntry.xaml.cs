using LabelVal.Dialogs;
using LabelVal.WindowViews;
using MahApps.Metro.Controls.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace LabelVal.ImageRolls.Views;

/// <summary>
/// Interaction logic for LabelControlView.xaml
/// </summary>
public partial class ImageResultEntry : UserControl
{
    public ImageResultEntry() => InitializeComponent();

    private ViewModels.ImageResultEntry viewModel;

    private void UserControl_Loaded(object sender, RoutedEventArgs e)
    {
        viewModel = (ViewModels.ImageResultEntry)DataContext;
        viewModel.BringIntoView += ViewModel_BringIntoView;
    }
    private void UserControl_Unloaded(object sender, RoutedEventArgs e)
    {
        DialogParticipation.SetRegister(this, null);

        viewModel.BringIntoView -= ViewModel_BringIntoView;
        viewModel = null;
    }

    private void ViewModel_BringIntoView() => App.Current.Dispatcher.Invoke(new Action(BringIntoView));





}
