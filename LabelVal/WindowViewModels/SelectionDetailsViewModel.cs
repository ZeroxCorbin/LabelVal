using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;

namespace LabelVal.WindowViewModels;
public partial class SelectionDetailsViewModel : ObservableRecipient, IRecipient<NodeMessages.SelectedNodeChanged>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty] private V275Node selectedNode;

    public SelectionDetailsViewModel() =>
        //Logger.Info("SelectionDetailsViewModel created");
        IsActive = true;

    public void Receive(NodeMessages.SelectedNodeChanged message) => SelectedNode = message.Value;
}

