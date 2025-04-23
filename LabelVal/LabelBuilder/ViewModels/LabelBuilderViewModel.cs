using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LabelVal.LabelBuilder.ViewModels
{
    public class LabelBuilderViewModel : ObservableObject
    {
        public BarcodeBuilder.lib.Wpf.ViewModels.BarcodeBuilderViewModel BarcodeBuilderViewModel { get; } = new BarcodeBuilder.lib.Wpf.ViewModels.BarcodeBuilderViewModel(App.Settings);


    }
}
