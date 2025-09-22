using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace LabelVal.Results.ViewModels;

public partial class ResultsManagerViewModel
{
    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool hasStoredResults;

    [CommunityToolkit.Mvvm.ComponentModel.ObservableProperty]
    private bool hasCurrentResults;

    public bool HasStoredAndCurrentResults => HasStoredResults && HasCurrentResults;

    partial void OnHasStoredResultsChanged(bool oldValue, bool newValue) =>
        OnPropertyChanged(nameof(HasStoredAndCurrentResults));
    partial void OnHasCurrentResultsChanged(bool oldValue, bool newValue) =>
        OnPropertyChanged(nameof(HasStoredAndCurrentResults));

    private CancellationTokenSource _presenceDebounceCts;

    /// <summary>
    /// Queue a debounced presence update (use this liberally after mutations).
    /// </summary>
    public void QueueUpdateResultsPresence(int delayMs = 50)
    {
        var cts = new CancellationTokenSource();
        var prev = Interlocked.Exchange(ref _presenceDebounceCts, cts);
        prev?.Cancel();

        // Use Background so UI stays responsive during bursts.
        _ = System.Windows.Application.Current.Dispatcher.InvokeAsync(async () =>
        {
            try
            {
                await Task.Delay(delayMs, cts.Token);
                UpdateResultsPresence();
            }
            catch (TaskCanceledException) { /* ignored */ }
        }, DispatcherPriority.Background);
    }

    /// <summary>
    /// Recompute aggregate flags immediately (normally use QueueUpdateResultsPresence()).
    /// </summary>
    public void UpdateResultsPresence()
    {
        if (!System.Windows.Application.Current.Dispatcher.CheckAccess())
        {
            System.Windows.Application.Current.Dispatcher.Invoke(UpdateResultsPresence);
            return;
        }

        bool anyStored = false;
        bool anyCurrent = false;

        if (ResultssEntries != null)
        {
            foreach (var entry in ResultssEntries)
            {
                var devices = entry?.ResultsDeviceEntries;
                if (devices == null) continue;

                foreach (var dev in devices)
                {
                    // We use reflection-friendly dynamic access because interface snippet didn't show these members formally.
                    // Assumes concrete implementations expose StoredSectors, CurrentSectors, StoredImage, CurrentImage.
                    try
                    {
                        dynamic d = dev;
                        if (!anyStored &&
                            (((int?)d.StoredSectors?.Count ?? 0) > 0 || d.StoredImage != null))
                            anyStored = true;

                        if (!anyCurrent &&
                            (((int?)d.CurrentSectors?.Count ?? 0) > 0 || d.CurrentImage != null))
                            anyCurrent = true;

                        if (anyStored && anyCurrent)
                            goto Done;
                    }
                    catch
                    {
                        // Ignore devices without expected members.
                    }
                }
            }
        }

Done:
        HasStoredResults = anyStored;
        HasCurrentResults = anyCurrent;
    }
}