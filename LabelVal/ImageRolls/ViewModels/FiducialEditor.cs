using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media.Imaging;

namespace LabelVal.ImageRolls.ViewModels;
public partial class FiducialEditor : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty] private BitmapImage image;

    public FiducialEditor(BitmapImage image) => this.image = image;

}
