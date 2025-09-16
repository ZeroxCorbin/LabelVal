using System.Diagnostics;
using System.Windows.Documents;
using System.Windows;
using MaterialDesignThemes.Wpf;
using System.Windows.Controls;

namespace LabelVal.LabelBuilder.Extensions
{
    public static class FrameworkElement_ClickProcessStart
    {
        public static bool GetIsExternal(DependencyObject obj)
        {
            if (IsExternalProperty == null)
                return false;
            return (bool)obj.GetValue(IsExternalProperty);
        }

        public static void SetIsExternal(DependencyObject obj, bool value)
        {
            if (IsExternalProperty == null)
                return;

            obj.SetValue(IsExternalProperty, value);
        }
        public static readonly DependencyProperty IsExternalProperty =
            DependencyProperty.RegisterAttached("IsExternal", typeof(bool), typeof(FrameworkElement_ClickProcessStart), new UIPropertyMetadata(false, OnIsExternalChanged));

        private static void OnIsExternalChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            if (sender is FrameworkElement control)
            {
                if ((bool)args.NewValue)
                    control.MouseLeftButtonDown += Control_Click;
                else
                    control.MouseLeftButtonDown -= Control_Click;
            }
            else
            {
                if (sender is FrameworkElement element)
                {
                    if ((bool)args.NewValue)
                        element.MouseLeftButtonDown += Control_Click;
                    else
                        element.MouseLeftButtonDown -= Control_Click;
                }
            }

        }

        private static void Control_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty((string)((FrameworkElement)sender).Tag))
            {
                e.Handled = true;
                return;
            }

            var ps = new ProcessStartInfo((string)((FrameworkElement)sender).Tag)
            {
                UseShellExecute = true,
                Verb = "open"
            };
            Process.Start(ps);

        }

    }
}
