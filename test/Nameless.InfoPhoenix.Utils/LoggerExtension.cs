using Microsoft.Extensions.Logging;
using Moq;

namespace Nameless.InfoPhoenix.Utils {
    public static class LoggerExtension {
        private static readonly Func<string, bool> EmptyAssertMessage = _ => true;

        public static Mock<ILogger<T>> VerifyDebug<T>(this Mock<ILogger<T>> self, Func<string, bool>? assertMessage = null, Times? times = null)
            => VerifyFor(self, assertMessage ?? EmptyAssertMessage, LogLevel.Debug, times);

        public static Mock<ILogger<T>> VerifyError<T>(this Mock<ILogger<T>> self, Func<string, bool>? assertMessage = null, Times? times = null)
            => VerifyFor(self, assertMessage ?? EmptyAssertMessage, LogLevel.Error, times);

        public static Mock<ILogger<T>> VerifyInformation<T>(this Mock<ILogger<T>> self, Func<string, bool>? assertMessage = null, Times? times = null)
            => VerifyFor(self, assertMessage ?? EmptyAssertMessage, LogLevel.Information, times);

        public static Mock<ILogger<T>> VerifyWarning<T>(this Mock<ILogger<T>> self, Func<string, bool>? assertMessage = null, Times? times = null)
            => VerifyFor(self, assertMessage ?? EmptyAssertMessage, LogLevel.Warning, times);

        public static Mock<ILogger<T>> VerifyFor<T>(this Mock<ILogger<T>> self, Func<string, bool>? assertMessage, LogLevel level = LogLevel.Debug, Times? times = null) {
            times ??= Times.Once();

            Func<object, Type, bool> state = (value, type)
                => (assertMessage ?? EmptyAssertMessage)(value.ToString() ?? string.Empty) && type.Name == "FormattedLogValues";

            self.Verify(mock => mock.Log(It.Is<LogLevel>(currentLogLevel => currentLogLevel == level),
                                         It.Is<EventId>(eventId => eventId.Id == 0),
                                         It.Is<It.IsAnyType>((value, type) => state(value, type)),
                                         It.IsAny<Exception>(),
                                         It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                        (Times)times);

            return self;
        }
    }
}
