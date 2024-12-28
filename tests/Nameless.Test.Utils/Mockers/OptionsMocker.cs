using Microsoft.Extensions.Options;

namespace Nameless.Test.Utils.Mockers;

public class OptionsMocker<T> : MockerBase<IOptions<T>> where T : class, new() {
    public OptionsMocker<T> WithDefaultValue() {
        InnerMock.Setup(mock => mock.Value)
                 .Returns(new T());

        return this;
    }

    public OptionsMocker<T> WithValue(T value) {
        InnerMock.Setup(mock => mock.Value)
                 .Returns(value);

        return this;
    }
}