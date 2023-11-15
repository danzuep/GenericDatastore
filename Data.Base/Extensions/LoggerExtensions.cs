using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Data.Base.Extensions;

/// <summary>
/// <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging"/>
/// </summary>
[ExcludeFromCodeCoverage]
public static class LoggerExtensions
{
    public static void LogResults<T>(this ILogger logger, T? obj, LogLevel logLevel = LogLevel.Information) where T : class =>
        logger.Log(logLevel, "\"{Name}\": {JsonSerializedObject}", typeof(T).Name, obj.ToSerializedString());

    private static readonly EventId _logEvent = new(id: 0, name: nameof(LogAction));

    public static void Log(this ILogger logger, string message, LogLevel logLevel = LogLevel.Information) =>
        LogAction(message, logLevel)(logger, null);

    public static void Log(this ILogger logger, Exception? ex, string message, LogLevel logLevel = LogLevel.Error) =>
        LogAction(message, logLevel)(logger, ex);

    public static Action<ILogger, Exception?> LogAction(string message, LogLevel logLevel) =>
        LoggerMessage.Define(logLevel, _logEvent, message);

    public static void Log<T>(this ILogger logger, LogLevel logLevel, string formatString, T parameter) =>
        LoggerMessage.Define<T>(logLevel, _logEvent, formatString)(logger, parameter, null);

    public static void Log<T>(this ILogger logger, Exception? ex, string formatString, T parameter, LogLevel logLevel = LogLevel.Error) =>
        LoggerMessage.Define<T>(logLevel, _logEvent, formatString)(logger, parameter, ex);

    public static void Log(this ILogger logger, Exception ex, LogLevel logLevel = LogLevel.Error) =>
        LoggerMessage.Define<string>(logLevel, _logEvent, "{ExceptionMessage}")(logger, ex.Message, ex);

    public static void Log<T1, T2>(this ILogger logger, LogLevel logLevel, string formatString, T1 param1, T2 param2) =>
        LoggerMessage.Define<T1, T2>(logLevel, _logEvent, formatString)(logger, param1, param2, null);

    public static void Log<T1, T2>(this ILogger logger, Exception? ex, LogLevel logLevel, string formatString, T1 param1, T2 param2) =>
        LoggerMessage.Define<T1, T2>(logLevel, _logEvent, formatString)(logger, param1, param2, ex);

    public static void Log<T1, T2, T3>(this ILogger logger, string formatString, T1 param1, T2 param2, T3 param3, LogLevel logLevel = LogLevel.Information) =>
        LoggerMessage.Define<T1, T2, T3>(logLevel, _logEvent, formatString)(logger, param1, param2, param3, null);

    public static void Log<T1, T2, T3>(this ILogger logger, Exception? ex, string formatString, T1 param1, T2 param2, T3 param3, LogLevel logLevel = LogLevel.Error) =>
        LoggerMessage.Define<T1, T2, T3>(logLevel, _logEvent, formatString)(logger, param1, param2, param3, ex);
}
