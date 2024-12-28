using Microsoft.Extensions.Logging;

namespace Nameless.Test.Utils.Mockers;

public class LoggerMocker<T> : MockerBase<ILogger<T>> {
    public LoggerMocker<T> WithLogLevel(params LogLevel[] levels) {
        foreach (var level in levels) {
            InnerMock.Setup(mock => mock.IsEnabled(level))
                     .Returns(true);
        }

        return this;
    }
}