using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using LabelVal.Messages;

namespace LabelVal.V275.ViewModels;
public partial class NodeDetails : ObservableRecipient, IRecipient<NodeMessages.SelectedNodeChanged>
{
    [ObservableProperty] private Node selectedNode;
    public void Receive(NodeMessages.SelectedNodeChanged message) => SelectedNode = message.Value;
}
