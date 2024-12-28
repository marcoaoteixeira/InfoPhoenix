using Microsoft.Extensions.Configuration;

namespace Nameless.Test.Utils.Mockers;

public class ConfigurationSectionMocker : MockerBase<IConfigurationSection> {
    public ConfigurationSectionMocker WithKey(string key) {
        InnerMock.Setup(mock => mock.Key)
                    .Returns(key);

        return this;
    }

    public ConfigurationSectionMocker WithValue(string value) {
        InnerMock.Setup(mock => mock.Value)
                    .Returns(value);

        return this;
    }
}