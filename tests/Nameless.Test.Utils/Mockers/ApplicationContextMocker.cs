using Nameless.Application;

namespace Nameless.Test.Utils.Mockers;

public class ApplicationContextMocker : MockerBase<IApplicationContext> {
    public ApplicationContextMocker WithEnvironment(string environment) {
        InnerMock.Setup(mock => mock.Environment).Returns(environment);

        return this;
    }

    public ApplicationContextMocker WithAppName(string appName) {
        InnerMock.Setup(mock => mock.AppName).Returns(appName);

        return this;
    }

    public ApplicationContextMocker WithAppFolderPath(string appFolderPath) {
        InnerMock.Setup(mock => mock.AppFolderPath).Returns(appFolderPath);

        return this;
    }

    public ApplicationContextMocker WithAppDataFolderPath(string appDataFolderPath) {
        InnerMock.Setup(mock => mock.AppDataFolderPath).Returns(appDataFolderPath);

        return this;
    }

    public ApplicationContextMocker WithVersion(string version) {
        InnerMock.Setup(mock => mock.Version).Returns(version);

        return this;
    }
}