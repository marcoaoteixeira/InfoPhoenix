using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Domains.Entities;
using Nameless.Test.Utils;
using Nameless.Test.Utils.Mockers;

namespace Nameless.InfoPhoenix.Domains.Bootstrap;

public class InitializeAppDbContextStepTests {
    [Test]
    [Ignore("Need fixing")]
    public async Task WhenExecuteAsync_WithRelationalDatabase_ThenExecuteDatabaseMigrationRoutine() {
        var services = new ServiceCollection();
        var loggerMocker = new LoggerMocker<InitializeAppDbContextStep>().WithLogLevel(LogLevel.Information);
        var loggerFactory = new LoggerFactoryMocker().WithLogger(loggerMocker.Build())
                                                     .Build();
        var applicationContext = new ApplicationContextMocker().WithAppDataFolderPath(TestUtilsConstants.BinFolderPath)
                                                               .Build();

        services.AddSingleton(loggerFactory);
        services.AddSingleton(applicationContext);
        services.RegisterEntityFramework();
        var provider = services.BuildServiceProvider();

        var sut = new InitializeAppDbContextStep(provider);

        await sut.ExecuteAsync();

        loggerMocker.VerifyInformationCall(message => message.Contains("Executing step") &&
                                                      message.Contains(sut.Name));

        loggerMocker.VerifyInformationCall(message => message.Contains("finished") &&
                                                      message.Contains(sut.Name));
    }

    [Test]
    [Ignore("Need fixing")]
    public async Task WhenExecuteAsync_WithNonRelationalDatabase_ThenSkipsMigrationRoutine() {
        var binFolder = typeof(InitializeAppDbContextStepTests).Assembly.GetDirectoryPath();

        var services = new ServiceCollection();
        var loggerMocker = new LoggerMocker<InitializeAppDbContextStep>().WithLogLevel(LogLevel.Information);
        var loggerFactory = new LoggerFactoryMocker().WithLogger(loggerMocker.Build())
                                                     .Build();
        var applicationContext = new ApplicationContextMocker().WithAppDataFolderPath(binFolder)
                                                               .Build();

        services.AddSingleton(loggerFactory);
        services.AddSingleton(applicationContext);
        services.AddDbContext<AppDbContext>(configure => configure.UseInMemoryDatabase("InMemory"));
        var provider = services.BuildServiceProvider();

        var sut = new InitializeAppDbContextStep(provider);

        await sut.ExecuteAsync();

        loggerMocker.VerifyInformationCall(message => message.Contains("Executing step") &&
                                                      message.Contains(sut.Name));

        loggerMocker.VerifyInformationCall(message => message.Contains("Skipping migration"));

        loggerMocker.VerifyInformationCall(message => message.Contains("finished") &&
                                                      message.Contains(sut.Name));
    }
}