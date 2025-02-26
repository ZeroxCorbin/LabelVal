using Logging.lib;
using System.Windows.Controls;

namespace LabelVal.Main.Views;

public partial class LoggingViewer : UserControl
{
    public LoggingViewer() => InitializeComponent();

    private void dataGrid_PreviewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        //Copy the selected row to the clipboard
        if (sender is DataGrid dataGrid && dataGrid.SelectedItem != null)
        {
            if(dataGrid.SelectedItem is not LoggerMessage msg)
                return;
            string row = $"{msg.TimeStamp}, {msg.Type}, {msg.Message}";

            System.Windows.Clipboard.SetText(row);
        }
    }
}
