using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Lvs95xx.lib.Shared.LvsDatabases;
using Lvs95xx.lib.Shared.Watchers;
using System;
using Watchers.lib.Process;
using Watchers.lib.Registry;

namespace Lvs95xx.Producer.Watchers;
public partial class ActiveWatchers : ObservableRecipient
{
    //private List<string> _hubs = ["VID_0424", "VID_0409"];

    ////VID_0424 has VID_199E and VID_0403 connected to it
    ////VID_0409 has VID_199E and VID_07F2 connected to it
    //private Dictionary<string, List<string>> _hubToDevice = new()
    //{
    //    ["VID_0424"] = ["VID_199E", "VID_0403"],
    //    ["VID_0409"] = ["VID_199E", "VID_07F2"]

    //};
    ////For the Dongle only
    //private List<string> _devices = ["VID_07F2"];

    //public List<DeviceRoot> RootDevices { get; set; } = [];
    //public List<string> Volumes { get; set; } = [];

    //private RemovableDriveWatcher _removableDriveWatcher = new();
    //private Win32_VolumeChangeEventWatcher _volumeChangeEventWatcher = new();

    //private List<Win32_PnPEntityWatcher> _pnPEntityWatchers = [];

    private Win32_ProcessWatcher _processWatcher = new();

    private RegistryTreeChangeEventWatcher _registryWatcher = new();

    private PasswordOfTheDayWatcher _passwordOfTheDayWatcher = new(new TimeSpan(24, 0, 0));

    /// <summary>
    /// <see cref="ActiveDatabase"/>
    /// </summary>
    [ObservableProperty] private LvsDatabase? activeDatabase;
    partial void OnActiveDatabaseChanged(LvsDatabase? value)
    {
        //StopActiveDatabaseWatcher();
        //if (value != null)
        //    StartActiveDatabaseWatcher(value.FilePath);

        //_ = WeakReferenceMessenger.Default.Send(new ActiveDatabaseChanged(value));
    }

    public ActiveWatchers()
    {

        WeakReferenceMessenger.Default.Register<RequestMessage<RegistryMessage>>(
            this,
            (recipient, message) =>
            {
                message.Reply(new RegistryMessage(_registryWatcher.GetRegistryValue()));
            });

        WeakReferenceMessenger.Default.Register<RequestMessage<PasswordOfTheDayMessage>>(
            this,
            (recipient, message) =>
            {
                message.Reply(new PasswordOfTheDayMessage(Lvs95xx.lib.Core.Controllers.Controller.GetTodaysPassword()));
            });

        WeakReferenceMessenger.Default.Register<RequestMessage<Win32_ProcessWatcherMessage>>(
            this,
            (recipient, message) =>
            {
                message.Reply(new Win32_ProcessWatcherMessage(_processWatcher.AppName, _processWatcher.MainWindowTitle, _processWatcher.Process, _processWatcher.State));
            });

        _registryWatcher.OnRegistryChanged += (value) =>
        {
            _ = WeakReferenceMessenger.Default.Send(new RegistryMessage(value));
        };
        _registryWatcher.Start(Microsoft.Win32.RegistryHive.LocalMachine, @"SOFTWARE\Microscan\LVS-95XX", "Database", updateOnStart: false);

        _processWatcher.OnProcessChanged += (appName, mainWindowTitle, state, process) =>
        {
            _ = WeakReferenceMessenger.Default.Send(new Win32_ProcessWatcherMessage(appName, mainWindowTitle, process, state));
        };
        _processWatcher.Start("LVS-95XX.exe");


        IsActive = true;
    }

    //private void StartUsbWatchers()
    //{
    //    foreach (string hub in _hubs)
    //    {
    //        LogDebug($"Starting watcher for hub: {hub}");

    //        Win32_PnPEntityWatcher watcher = new();
    //        watcher.Start("DeviceID", hub, HubChangeCallback);
    //        _pnPEntityWatchers.Add(watcher);
    //    }

    //    foreach (string device in _devices)
    //    {
    //        LogDebug($"Starting watcher for device: {device}");

    //        Win32_PnPEntityWatcher watcher = new();
    //        watcher.Start("DeviceID", device, DeviceChangeCallback);
    //        _pnPEntityWatchers.Add(watcher);
    //    }

    //    //LogDebug("Starting watcher for all devices");

    //    //Win32_PnPEntityWatcher all = new();
    //    //all.Start("DeviceID", "VID_", AllDeviceChangeCallback, false);
    //    //_pnPEntityWatchers.Add(all);
    //}

    //private void HubChangeCallback(bool add, ManagementBaseObject? device)
    //{
    //    if (device == null || device["DeviceID"].ToString() is not string deviceID)
    //        return;

    //    string? hubVid = null;
    //    foreach (string hub in _hubs)
    //        if (deviceID.Contains(hub))
    //        {
    //            hubVid = hub;
    //            break;
    //        }
    //    if (hubVid == null)
    //        return;

    //    DeviceRoot? node = new(device, _hubToDevice[hubVid]);
    //    if (node == null)
    //        return;

    //    UpdateRootDevices(node, add);
    //}
    //private void DeviceChangeCallback(bool add, ManagementBaseObject? device)
    //{
    //    if (device == null)
    //        return;

    //    DeviceNode node = new(device);

    //    DeviceNode? res = GetRoot(node, "Hub");
    //    if (res != null)
    //        foreach (string hub in _hubs)
    //            if (res.DeviceID.Contains(hub))
    //                return;

    //    UpdateRootDevices(node, add);
    //}
    //private void UpdateRootDevices(DeviceNode deviceNode, bool add)
    //{
    //    DeviceRoot? dev = RootDevices.FirstOrDefault(x => x.DeviceID == deviceNode.DeviceID);
    //    if (dev != null)
    //    {
    //        _ = RootDevices.Remove(dev);
    //        _ = WeakReferenceMessenger.Default.Send(new UsbDeviceMessage(dev, false));

    //        LogDebug($"Removed device: {dev.DeviceID}");

    //        dev.Dispose();
    //    }

    //    if (add)
    //    {
    //        DeviceRoot root = new(deviceNode.ManagementBaseObject, []);
    //        RootDevices.Add(root);
    //        _ = WeakReferenceMessenger.Default.Send(new UsbDeviceMessage(root, true));

    //        LogDebug($"Added device: {root.DeviceID}");
    //    }
    //}

    //private void UpdateRootDevices(DeviceRoot deviceRoot, bool add)
    //{
    //    DeviceRoot? dev = RootDevices.FirstOrDefault(x => x.DeviceID == deviceRoot.DeviceID);
    //    if (dev != null)
    //    {

    //        _ = RootDevices.Remove(dev);
    //        _ = WeakReferenceMessenger.Default.Send(new UsbDeviceMessage(dev, false));

    //        LogDebug($"Removed device: {dev.DeviceID}");

    //        dev.Dispose();
    //    }

    //    if (add)
    //    {
    //        RootDevices.Add(deviceRoot);
    //        _ = WeakReferenceMessenger.Default.Send(new UsbDeviceMessage(deviceRoot, true));

    //        LogDebug($"Added device: {deviceRoot.DeviceID}");
    //    }
    //}

    //private void VolumeChangedCallback(bool add, string volume)
    //{
    //    if (add)
    //    {
    //        if (!Volumes.Contains(volume))
    //        {
    //            _ = WeakReferenceMessenger.Default.Send(new VolumeMessage(true, volume));
    //            Volumes.Add(volume);

    //            LogDebug($"Added volume: {volume}");
    //        }
    //    }
    //    else
    //    {
    //        if (Volumes.Contains(volume))
    //        {
    //            _ = WeakReferenceMessenger.Default.Send(new VolumeMessage(false, volume));
    //            _ = Volumes.RemoveAll((v) => v == volume);

    //            LogDebug($"Removed volume: {volume}");
    //        }
    //    }
    //}

    //private void AllDeviceChangeCallback(bool add, ManagementBaseObject? device)
    //{
    //    if (device == null)
    //        return;

    //    if (add)
    //    {
    //        DeviceNode node = new(device);
    //        DeviceNode? res = GetRoot(node, "Hub");
    //        if (res == null)
    //            return;

    //        // Check if the root device is a hub
    //        bool isHub = _hubs.Any(res.DeviceID.Contains);
    //        if (isHub)
    //        {
    //            DeviceRoot? dev = RootDevices.FirstOrDefault(x => x.DeviceID == res.DeviceID);
    //            dev?.RefreshChildren();

    //            LogDebug($"Refreshed hub: {dev?.DeviceID}");
    //        }
    //    }
    //    else
    //    {

    //    }
    //}

    //private DeviceNode? GetRoot(DeviceNode deviceNode, string deviceName, int maxDepth = 2, int depth = 0)
    //{
    //    if (deviceNode == null || deviceNode.UsbDevice?.ParentPnpDeviceId == null)
    //        return deviceNode;

    //    string parentPnpDeviceId = deviceNode.UsbDevice.ParentPnpDeviceId.Replace("\\", "\\\\");

    //    using ManagementObjectSearcher searcher = new($"SELECT * FROM Win32_PnPEntity WHERE DeviceID = '{parentPnpDeviceId}'");

    //    foreach (ManagementBaseObject parent in searcher.Get())
    //    {
    //        DeviceNode parentNode = new(parent);
    //        return parent["Name"].ToString() is not string name
    //            ? null
    //            : name.Contains(deviceName) ? parentNode : GetRoot(parentNode, deviceName, maxDepth, depth++);
    //    }

    //    return deviceNode;
    //}

    #region Logging
    private void LogInfo(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
#if DEBUG
    private void LogDebug(string message) => Logging.lib.Logger.LogDebug(GetType(), message);
#else
    private void LogDebug(string message) { }
#endif
    private void LogWarning(string message) => Logging.lib.Logger.LogInfo(GetType(), message);
    private void LogError(string message) => Logging.lib.Logger.LogError(GetType(), message);
    private void LogError(Exception ex) => Logging.lib.Logger.LogError(GetType(), ex);
    private void LogError(Exception ex, string message) => Logging.lib.Logger.LogError(GetType(), ex, message);

    #endregion

}
