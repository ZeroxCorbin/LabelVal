using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;

namespace LabelVal.Main.ViewModels
{
    public class TemplateSelector : DataTemplateSelector
    {
        public DataTemplate Run { get; set; }
        public DataTemplate Printer { get; set; }
        public DataTemplate V275 { get; set; }
        public DataTemplate V5 { get; set; }
        public DataTemplate L96XX { get; set; }
        public DataTemplate ImageResultsDatabases { get; set; }
        public DataTemplate Seperator { get; set; }
        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var hmi = item as HamburgerMenuItem;
           
            //This is used to catch is the request if for Content versus The menu Item.
            if(hmi == null)
                hmi = new HamburgerMenuItem { Content = item };

            if (hmi != null)
            {
                if (hmi.Content is V275.ViewModels.V275Manager)
                {
                    return V275;
                }
                else if (hmi.Content is V5.ViewModels.ScannerManager)
                {
                    return V5;
                }
                else if (hmi.Content is LVS_95xx.ViewModels.VerifierManager)
                {
                    return L96XX;
                }
                else if (hmi.Content is Printer.ViewModels.Printer)
                {
                    return Printer;
                }
                else if (hmi.Content is Results.ViewModels.ImageResultsDatabases)
                {
                    return ImageResultsDatabases;
                }
                else if (hmi.Content is Run.ViewModels.RunControl)
                {
                    return Run;
                }
                else
                {

                }

            }

            return base.SelectTemplate(item, container);
        }
    }

}
