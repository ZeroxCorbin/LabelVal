using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.L95.ViewModels;
using LabelVal.Results.ViewModels;
using LabelVal.V275.ViewModels;
using LabelVal.V5.ViewModels;
using System;
using System.Collections.ObjectModel;

namespace LabelVal.Run.ViewModels;
public partial class RunControl : ObservableObject
{
    public Controller.Controller RunController { get; } = new();

    public ObservableCollection<ResultsEntry> ResultssEntries { get; private set; }

    public Node V275 { get; private set; }
    public Scanner V5 { get; private set; }
public Verifier L95 { get; private set; }

    public ImageRoll ActiveImageRoll { get; private set; }

    private int LoopCount { get; set; }

    /// <summary>
    /// If using this constructor, you must call Update before StartStop.
    /// </summary>
    public RunControl() { }

    public RunControl(int loopCount, ObservableCollection<ResultsEntry> imageResults, ImageRoll imageRollEntry, Node v275, Scanner v5, Verifier l95)
    {
        LoopCount = loopCount;
        ResultssEntries = imageResults;
        ActiveImageRoll = imageRollEntry;
        V275 = v275;
        V5 = v5;
        L95 = l95;
    }

    public void Update(int loopCount, ObservableCollection<ResultsEntry> imageResults, ImageRoll imageRollEntry, Node v275, Scanner v5, Verifier l95)
    {
        if (RunController.State == RunStates.Running)
        {
            Logger.Debug("Cannot update RunControl while running");
            return;
        }

        LoopCount = loopCount;
        ResultssEntries = imageResults;
        ActiveImageRoll = imageRollEntry;
        V275 = v275;
        V5 = v5;
        L95 = l95;
    }

    [RelayCommand]
    private void StartStop()
    {
        if (RunController == null || ActiveImageRoll == null)
            return;

        if (RunController.State == RunStates.Running)
        {
            Logger.Info($"Stopping Run: {ActiveImageRoll.Name}; {LoopCount.ToString()}");
            RunController.Stop();
        }
        else
        {
            Logger.Info($"Starting Run: {ActiveImageRoll.Name}; {LoopCount.ToString()}");
            RunController.StartAsync(ResultssEntries, ActiveImageRoll, V275, V5?.Controller, L95?.Controller, LoopCount);
        }
    }

    [RelayCommand]
    private void Reset()
    {
        if (RunController == null)
            return;

        RunController.Reset();
    }

}
