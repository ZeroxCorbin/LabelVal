using MahApps.Metro.Controls.Dialogs;
using System;
using System.Windows;
using System.Windows.Controls;

namespace LabelVal.Results.Views;

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

    private void btnMove_Click(object sender, RoutedEventArgs e)
    {
        popMove.PlacementTarget = sender as UIElement;
        popMove.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
        popMove.StaysOpen = false;
        popMove.IsOpen = true;
    }

    private void btnMoveImage_Click(object sender, RoutedEventArgs e)
    {
        popMove.IsOpen = false;
        ViewModels.ImageResultEntry viewModel = (ViewModels.ImageResultEntry)DataContext;
        if (((Button)sender).Tag is string s)
            switch (s)
            {
                case "top":
                    viewModel.ImageResults.MoveImageTopCommand.Execute(viewModel);
                    break;
                case "up":
                    viewModel.ImageResults.MoveImageUpCommand.Execute(viewModel);
                    break;
                case "down":
                    viewModel.ImageResults.MoveImageDownCommand.Execute(viewModel);
                    break;
                case "bottom":
                    viewModel.ImageResults.MoveImageBottomCommand.Execute(viewModel);
                    break;
            }

        BringIntoView();
    }

    private void btnAdd_Click(object sender, RoutedEventArgs e)
    {
        popAdd.PlacementTarget = sender as UIElement;
        popAdd.Placement = System.Windows.Controls.Primitives.PlacementMode.Center;
        popAdd.StaysOpen = false;
        popAdd.IsOpen = true;
    }

    private void btnAddImage_Click(object sender, RoutedEventArgs e)
    {
        popAdd.IsOpen = false;
        ViewModels.ImageResultEntry viewModel = (ViewModels.ImageResultEntry)DataContext;
        if (((Button)sender).Tag is string s)
            switch (s)
            {
                case "top":
                    viewModel.ImageResults.AddImageTopCommand.Execute(viewModel);
                    break;
                case "up":
                    viewModel.ImageResults.AddImageAboveCommand.Execute(viewModel);
                    break;
                case "down":
                    viewModel.ImageResults.AddImageBelowCommand.Execute(viewModel);
                    break;
                case "bottom":
                    viewModel.ImageResults.AddImageBottomCommand.Execute(viewModel);
                    break;
            }
    }
}
