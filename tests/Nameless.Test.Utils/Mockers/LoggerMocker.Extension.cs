using Microsoft.Extensions.Logging;
using Moq;

namespace Nameless.Test.Utils.Mockers;

public static class LoggerMockerExtension {
    private static readonly Func<string, bool> EmptyAssertMessage = _ => true;

    public static MockerBase<ILogger<T>> VerifyDebugCall<T>(this MockerBase<ILogger<T>> self, Func<string, bool>? assertMessage = null, Times times = default)
        => self.VerifyCallFor(assertMessage ?? EmptyAssertMessage, LogLevel.Debug, times);

    public static MockerBase<ILogger<T>> VerifyErrorCall<T>(this MockerBase<ILogger<T>> self, Func<string, bool>? assertMessage = null, Times times = default)
        => self.VerifyCallFor(assertMessage ?? EmptyAssertMessage, LogLevel.Error, times);

    public static MockerBase<ILogger<T>> VerifyInformationCall<T>(this MockerBase<ILogger<T>> self, Func<string, bool>? assertMessage = null, Times times = default)
        => self.VerifyCallFor(assertMessage ?? EmptyAssertMessage, LogLevel.Information, times);

    public static MockerBase<ILogger<T>> VerifyWarningCall<T>(this MockerBase<ILogger<T>> self, Func<string, bool>? assertMessage = null, Times times = default)
        => self.VerifyCallFor(assertMessage ?? EmptyAssertMessage, LogLevel.Warning, times);

    public static MockerBase<ILogger<T>> VerifyCallFor<T>(this MockerBase<ILogger<T>> self, Func<string, bool>? assertMessage, LogLevel level = LogLevel.Debug, Times times = default) {
        Func<object, Type, bool> state = (value, type)
            => (assertMessage ?? EmptyAssertMessage)(value.ToString() ?? string.Empty) && type.Name.Contains("LogValues", StringComparison.OrdinalIgnoreCase);

        self.Verify(mock => mock.Log(It.Is<LogLevel>(currentLogLevel => currentLogLevel == level),
                                     It.Is<EventId>(eventId => eventId.Id == 0),
                                     It.Is<It.IsAnyType>((value, type) => state(value, type)),
                                     It.IsAny<Exception?>(),
                                     It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                    times);

        return self;
    }
}