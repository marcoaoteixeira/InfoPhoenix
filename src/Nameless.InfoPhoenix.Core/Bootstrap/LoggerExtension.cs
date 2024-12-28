using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Bootstrap;

public static class LoggerExtension {
    private static readonly Action<ILogger<IStep>, int, string, Exception?> StepExecutionStartingDelegate
        = LoggerMessage.Define<int, string>(logLevel: LogLevel.Information,
                                            eventId: default,
                                            formatString: "[Step: {Order}] Starting step: '{Name}'",
                                            options: null);

    public static void StepExecutionStarting(this ILogger<IStep> self, IStep step)
        => StepExecutionStartingDelegate(self, step.Order, step.Name, null /* exception */);

    private static readonly Action<ILogger<IStep>, int, string, Exception?> StepExecutionFinishedDelegate
        = LoggerMessage.Define<int, string>(logLevel: LogLevel.Information,
                                            eventId: default,
                                            formatString: "[Step: {Order}] Step finished: '{Name}'",
                                            options: null);

    public static void StepExecutionFinished(this ILogger<IStep> self, IStep step)
        => StepExecutionFinishedDelegate(self, step.Order, step.Name, null /* exception */);

    private static readonly Action<ILogger<IStep>, string, Exception> StepExecutionFailureDelegate
        = LoggerMessage.Define<string>(logLevel: LogLevel.Error,
                                       eventId: default,
                                       formatString: "An error occurred while executing step '{StepName}'",
                                       options: null);

    public static void StepExecutionFailure(this ILogger<IStep> self, IStep step, Exception exception)
        => StepExecutionFailureDelegate(self, step.Name, exception);
}