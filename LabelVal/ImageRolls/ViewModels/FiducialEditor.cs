using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace LabelVal.ImageRolls.ViewModels;
public partial class FiducialEditor : ObservableObject
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty] BitmapImage image;

    public FiducialEditor(BitmapImage image) => this.image = image;


}
