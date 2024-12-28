using Nameless.InfoPhoenix.Bootstrap;
using Nameless.InfoPhoenix.Configuration;

namespace Nameless.InfoPhoenix.Infrastructure.Bootstrap;

public sealed class InitializeAppConfigurationManagerStep : IStep {
    private readonly IAppConfigurationManager _appConfigurationManager;

    public string Name => "Initializing InfoPhoenix Application Configuration Manager";

    public int Order => 0;

    public bool ThrowOnFailure => false;

    public bool Skip => false;

    public InitializeAppConfigurationManagerStep(IAppConfigurationManager appConfigurationManager) {
        _appConfigurationManager = Prevent.Argument.Null(appConfigurationManager);
    }

    public Task ExecuteAsync() {
        _appConfigurationManager.Initialize();

        return Task.CompletedTask;
    }
}