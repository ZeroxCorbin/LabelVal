using NLog;
using System;
using CommunityToolkit.Mvvm.Messaging;

namespace LabelVal.Logging;

public class Logger : ILogger
{
    private static IMessenger Messenger => WeakReferenceMessenger.Default;
    private static NLog.Logger GetLogger(Type sourceType) => LogManager.GetLogger(sourceType.FullName);

    public void LogInfo(Type sourceType, string message)
    {
        var logger = GetLogger(sourceType);
        logger.Info(message);
        _ = Messenger.Send(new Messages.SystemMessages.StatusMessage(message, Messages.SystemMessages.StatusMessageType.Info));
    }

    public void LogDebug(Type sourceType, string message)
    {
        var logger = GetLogger(sourceType);
        logger.Debug(message);
        _ = Messenger.Send(new Messages.SystemMessages.StatusMessage(message, Messages.SystemMessages.StatusMessageType.Debug));
    }

    public void LogWarning(Type sourceType, string message)
    {
        var logger = GetLogger(sourceType);
        logger.Warn(message);
        _ = Messenger.Send(new Messages.SystemMessages.StatusMessage(message, Messages.SystemMessages.StatusMessageType.Warning));
    }

    public void LogError(Type sourceType, string message)
    {
        var logger = GetLogger(sourceType);
        logger.Error(message);
        _ = Messenger.Send(new Messages.SystemMessages.StatusMessage(message, Messages.SystemMessages.StatusMessageType.Error));
    }

    public void LogError(Type sourceType, Exception ex)
    {
        var logger = GetLogger(sourceType);
        logger.Error(ex);
        _ = Messenger.Send(new Messages.SystemMessages.StatusMessage(ex));
    }

    public void LogError(Type sourceType, string message, Exception ex)
    {
        var logger = GetLogger(sourceType);
        logger.Error(ex, message);
        _ = Messenger.Send(new Messages.SystemMessages.StatusMessage(message, ex));
    }
}
