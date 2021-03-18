using Funcs_DataMovement.Models;
using Microsoft.Extensions.Logging;
using Serilog.Context;
using Serilog.Events;

namespace Funcs_DataMovement.Logging
{
    public static class LoggingExtensions
    {
        public static void LoggerError(this ILogger log, string msg, string stacktrace, JPOFileInfo entry)
        {
            using (LogContext.PushProperty("producer", entry.source))
            using (LogContext.PushProperty("consumer", entry.destination))
            using (LogContext.PushProperty("file", entry.fileName))
            using (LogContext.PushProperty("origin", entry.origin))
            using (LogContext.PushProperty("tags", entry.tags))
            using (LogContext.PushProperty("correlationId", entry.correlationId))
            using (LogContext.PushProperty("stacktrace", stacktrace))
            {
                log.LogError(msg);
            }
        }

        public static void LoggerInfo(this ILogger log, string msg, JPOFileInfo entry)
        {
            log.LogIt(msg, entry, LogEventLevel.Information);
        }

        public static void LoggerWarning(this ILogger log, string msg, JPOFileInfo entry)
        {
            log.LogIt(msg, entry, LogEventLevel.Warning);
        }

        public static void LoggerDebug(this ILogger log, string msg, JPOFileInfo entry)
        {
            log.LogIt(msg, entry, LogEventLevel.Debug);
        }

        public static void LogIt(this ILogger log, string msg, JPOFileInfo entry, LogEventLevel level)
        {
            using (LogContext.PushProperty("producer", entry.source))
            using (LogContext.PushProperty("consumer", entry.destination))
            using (LogContext.PushProperty("file", entry.fileName))
            using (LogContext.PushProperty("origin", entry.origin))
            using (LogContext.PushProperty("tags", entry.tags))
            using (LogContext.PushProperty("correlationId", entry.correlationId))
            {
                switch (level)
                {
                    case LogEventLevel.Debug:
                        log.LogDebug(msg);
                        break;
                    case LogEventLevel.Warning:
                        log.LogWarning(msg);
                        break;
                    default:
                        log.LogInformation(msg);
                        break;
                }
            }
        }
    }
}
