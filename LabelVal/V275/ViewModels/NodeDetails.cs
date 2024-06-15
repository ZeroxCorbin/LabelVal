using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.ImageRolls.ViewModels;
using LabelVal.Messages;

namespace LabelVal.V275.ViewModels;
public partial class NodeDetails : ObservableRecipient, IRecipient<PropertyChangedMessage<Node>>
{
    [ObservableProperty] private Node selectedNode;
    public NodeDetails() => IsActive = true;
    public void Receive(PropertyChangedMessage<Node> message) => SelectedNode = message.NewValue;
}
