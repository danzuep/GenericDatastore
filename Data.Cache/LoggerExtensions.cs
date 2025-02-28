using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Data.Cache
{
    /// <summary>
    /// Provides extension methods for logging.
    /// <see href="https://learn.microsoft.com/en-us/dotnet/core/extensions/high-performance-logging"/>
    /// </summary>
    [ExcludeFromCodeCoverage]
    internal static class LoggerExtensions
    {
        /// <summary>
        /// Defines a log action with a message and log level.
        /// </summary>
        /// <param name="message">The message template for logging.</param>
        /// <param name="logLevel">The log level for the message.</param>
        /// <returns>An action that takes an ILogger and an Exception.</returns>
        private static Action<ILogger<T>, Exception> LogAction<T>(string message, LogLevel logLevel, int eventId) =>
            LoggerMessage.Define(logLevel, new EventId(eventId, typeof(T).FullName), message);

        /// <summary>
        /// Logs a message at the specified log level.
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="logLevel">The log level for the message. Default is Information.</param>
        public static void Log<T>(this ILogger<T> logger, string message, LogLevel logLevel = LogLevel.Information, int eventId = 0)
        {
            if (logger == null)
            {
                return;
            }
            LogAction<T>(message, logLevel, eventId)(logger, null);
        }

        /// <summary>
        /// Logs an exception with a message at the specified log level.
        /// </summary>
        /// <param name="logger">The logger to write to.</param>
        /// <param name="ex">The exception to log.</param>
        /// <param name="message">The message to log.</param>
        /// <param name="logLevel">The log level for the message. Default is Error.</param>
        public static void Log<T>(this ILogger<T> logger, Exception ex, string message, LogLevel logLevel = LogLevel.Error, int eventId = 1)
        {
            if (logger == null)
            {
                return;
            }
            LogAction<T>(message, logLevel, eventId)(logger, ex);
        }
    }
}
