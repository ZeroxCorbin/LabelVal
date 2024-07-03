using System;

namespace LabelVal.Logging;

public interface ILogger
{
    void LogInfo(Type sourceType, string message);
    void LogDebug(Type sourceType, string message);
    void LogWarning(Type sourceType, string message);
    void LogError(Type sourceType, string message);
    void LogError(Type sourceType, Exception ex);
    void LogError(Type sourceType, string message, Exception ex);
}