using LabelVal.Logging.Messages;
using RingBuffer.lib;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Data;

namespace LabelVal.Main.Views;
/// <summary>
/// Interaction logic for StatusMessageViewer.xaml
/// </summary>
public partial class StatusMessageViewer : UserControl
{
    public ICollectionView FilteredMessagesView { get; set; }

    public StatusMessageViewer()
    {
        InitializeComponent();

        Loaded += (s, e) =>
        {
            if (DataContext is RingBufferCollection<SystemMessages.StatusMessage> viewModel)
            {
                // Ensure the DataGrid's ItemsSource is bound to the collection in your ViewModel.
                // This might already be done via XAML binding.
                dataGrid.ItemsSource = viewModel;

                // Initialize the FilteredMessagesView with the DataGrid's ItemsSource.
                FilteredMessagesView = CollectionViewSource.GetDefaultView(dataGrid.ItemsSource);

                // Apply the filter.
                FilteredMessagesView.Filter = MessageFilter;

                // No need to re-assign DataContext here unless there's a specific reason to do so.
            }
        };
    }

    private bool MessageFilter(object item)
    {
        var message = item as SystemMessages.StatusMessage; // Replace YourMessageType with the actual type of the items in your collection
        if (message != null)
        {
            // Adjust the condition based on your requirements for the Type value
            return true;//message.Value != SystemMessages.StatusMessageType.Debug;
        }
        return false;
    }
}
