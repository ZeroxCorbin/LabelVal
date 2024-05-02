using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.V275.ViewModels;

namespace LabelVal.Messages;

public class NodeMessages
{

    public class SelectedNodeChanged(Node newNode, Node oldNode) : ValueChangedMessage<Node>(newNode)
    {
        public Node OldNode { get; } = oldNode;
    }
}
