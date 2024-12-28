using System.Text.Json;
using Microsoft.Extensions.Logging;
using Moq;
using Nameless.InfoPhoenix.Configuration;
using Nameless.Test.Utils;
using Nameless.Test.Utils.Mockers;

namespace Nameless.InfoPhoenix.Infrastructure.Configuration;

public class AppConfigurationContextTests {
    [Test]
    public void WhenInitialize_ThenReturnValuesFromConfigurationFile() {
        var expectedAppConfiguration = new AppConfiguration(Theme: Theme.HighContrast,
                                                            SearchHistoryLimit: SearchHistoryLimit.Medium,
                                                            SearchHistory: ["a", "b", "c"],
                                                            ConfirmBeforeExit: false,
                                                            EnableDocumentViewer: true);
        var appConfigurationJson = JsonSerializer.Serialize(expectedAppConfiguration);
        var configFilePath = Path.Combine(TestUtilsConstants.BinFolderPath, "info_phoenix.config");
        var fileInfoMocker = new FileInfoMocker().WithPhysicalPath(configFilePath)
                                                 .WithExists(true)
                                                 .WithCreateReadStream(appConfigurationJson);
        var fileProviderMocker = new FileProviderMocker().WithFileInfo(fileInfoMocker.Build());
        var loggerMocker =
            new LoggerMocker<AppConfigurationManager>().WithLogLevel(LogLevel.Error, LogLevel.Information);

        var sut = new AppConfigurationManager(fileProviderMocker.Build(), loggerMocker.Build());

        // act
        sut.Initialize();

        // assert
        Assert.Multiple(() => {
            fileProviderMocker.Verify(mock => mock.GetFileInfo(It.IsAny<string>()));
            fileInfoMocker.Verify(mock => mock.Exists);
            fileInfoMocker.Verify(mock => mock.CreateReadStream());

            Assert.That(sut.GetTheme(), Is.EqualTo(expectedAppConfiguration.Theme));
            Assert.That(sut.GetSearchHistoryLimit(), Is.EqualTo(expectedAppConfiguration.SearchHistoryLimit));
            Assert.That(sut.GetSearchHistory(), Is.EquivalentTo(expectedAppConfiguration.SearchHistory));
            Assert.That(sut.GetConfirmBeforeExit(), Is.EqualTo(expectedAppConfiguration.ConfirmBeforeExit));
            Assert.That(sut.GetEnableDocumentViewer(), Is.EqualTo(expectedAppConfiguration.EnableDocumentViewer));
        });
    }
}