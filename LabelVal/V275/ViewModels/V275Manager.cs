using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.ObjectModel;

namespace LabelVal.V275.ViewModels;

public partial class V275Manager : ObservableRecipient, IRecipient<PropertyChangedMessage<Node>>
{
    public ObservableCollection<NodeManager> Available { get; } = [];

    [ObservableProperty] private Node selectedNode;

    public V275Manager()
    {
        Available.Add(new NodeManager());
        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<Node> message) => SelectedNode = message.NewValue;
}
