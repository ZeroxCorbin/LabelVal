using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;

namespace LabelVal.V275.ViewModels;
public partial class NodeDetails : ObservableRecipient, IRecipient<NodeMessages.SelectedNodeChanged>
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

    [ObservableProperty] private Node selectedNode;

    public NodeDetails() =>
        //Logger.Info("SelectionDetailsViewModel created");
        IsActive = true;

    public void Receive(NodeMessages.SelectedNodeChanged message) => SelectedNode = message.Value;
}

