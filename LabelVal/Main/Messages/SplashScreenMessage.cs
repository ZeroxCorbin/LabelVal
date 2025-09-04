using CommunityToolkit.Mvvm.Messaging.Messages;

namespace LabelVal.Main.Messages;

/// <summary>
/// A message to be sent to the splash screen to update its status text.
/// </summary>
public class SplashScreenMessage(string message) : ValueChangedMessage<string>(message);