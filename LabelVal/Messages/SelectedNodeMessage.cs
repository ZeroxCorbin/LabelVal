using CommunityToolkit.Mvvm.Messaging.Messages;
using LabelVal.WindowViewModels;

namespace LabelVal.Messages
{
    public class NodeMessages
    {
        
        public class SelectedNodeChanged(V275Node newNode, V275Node oldNode) : ValueChangedMessage<V275Node>(newNode)
        {
            public V275Node OldNode { get; } = oldNode;
        }
    }
}
