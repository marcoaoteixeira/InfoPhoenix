using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Nameless.Test.Utils.Mockers;

public class LoggerFactoryMocker : MockerBase<ILoggerFactory> {
    public LoggerFactoryMocker() {
        // This will ensure that all other CreateLogger's calls
        // that were not covered by WithLogger return a
        // NullLogger instance, at least.
        InnerMock.Setup(mock => mock.CreateLogger(It.IsAny<string>()))
                 .Returns(NullLogger.Instance);
    }

    public LoggerFactoryMocker WithLogger<T>(ILogger<T> logger) {
        InnerMock.Setup(mock => mock.CreateLogger(typeof(T).FullName ?? typeof(T).Name))
                 .Returns(logger);

        return this;
    }
}