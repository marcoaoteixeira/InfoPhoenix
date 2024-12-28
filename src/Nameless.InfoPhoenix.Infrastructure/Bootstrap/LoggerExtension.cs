using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Infrastructure.Bootstrap;

internal static class LoggerExtension {
    private static readonly Action<ILogger<Bootstrapper>, Exception> StopBootstrapperDueStepExecutionFailureDelegate
        = LoggerMessage.Define(logLevel: LogLevel.Error,
                               eventId: default,
                               formatString: "Bootstrapper execution stopped due step execution failure.",
                               options: null);

    internal static void StopBootstrapperDueStepExecutionFailure(this ILogger<Bootstrapper> self, Exception exception)
        => StopBootstrapperDueStepExecutionFailureDelegate(self, exception);
}