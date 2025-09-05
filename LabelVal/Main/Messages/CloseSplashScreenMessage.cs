namespace LabelVal.Main.Messages;

/// <summary>
/// A message to signal the splash screen to close.
/// </summary>
public class CloseSplashScreenMessage(bool noDelay = false)
{
    /// <summary>
    /// If true, the splash screen should close immediately without any delay.
    /// </summary>
    public bool NoDelay { get; } = noDelay;
}