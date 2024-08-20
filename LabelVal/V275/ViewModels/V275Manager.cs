using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using System.Collections.ObjectModel;

namespace LabelVal.V275.ViewModels;

public partial class V275Manager : ObservableRecipient, IRecipient<PropertyChangedMessage<Node>>
{
    public ObservableCollection<NodeManager> NodeManagers { get; } = App.Settings.GetValue(nameof(NodeManagers), new ObservableCollection<NodeManager>(), true);

    [ObservableProperty] private NodeManager newNodeManager;    

    [ObservableProperty] private Node selectedNode;

    public V275Manager()
    {
        foreach (var nm in NodeManagers)
        {
nm.Manager = this;
            nm.GetDevicesCommand.Execute(null);
        }
            

        IsActive = true;
    }

    public void Receive(PropertyChangedMessage<Node> message) => SelectedNode = message.NewValue;

    [RelayCommand]
    private void Add() => NewNodeManager = new NodeManager() { Manager = this };
    //[RelayCommand]
    //private void Edit() => NewNodeManager = SelectedNode.Manager;
    [RelayCommand]
    private void Cancel() => NewNodeManager = null;
    [RelayCommand]
    private void Delete()
    {
        NodeManagers.Remove(NewNodeManager);
        NewNodeManager = null;
        Save();
    }
    [RelayCommand]
    private void Save()
    {
        if (NewNodeManager != null && !NodeManagers.Contains(NewNodeManager))
            NodeManagers.Add(NewNodeManager);

        NewNodeManager = null;

        App.Settings.SetValue(nameof(NodeManagers), NodeManagers);
    }

}
