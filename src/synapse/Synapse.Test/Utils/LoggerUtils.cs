using Microsoft.Extensions.Logging;

namespace Synapse.Test.Utils;

public static class LoggerUtils
{
    public static ILogger<T> GetConsoleLogger<T>()
    {
        using var loggerFactory = LoggerFactory.Create(builder =>
        {
            builder
                .SetMinimumLevel(LogLevel.Debug)
                .AddSimpleConsole(options =>
                {
                    options.IncludeScopes = true;
                    options.SingleLine = true;
                    options.TimestampFormat = "HH:mm:ss ";
                });
        });
        return loggerFactory.CreateLogger<T>();
    }
}
