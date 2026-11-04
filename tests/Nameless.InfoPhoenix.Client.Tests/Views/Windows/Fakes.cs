using Nameless.Application;
using Nameless.IO;
using Nameless.Windows.Bootstrap;
using Nameless.Windows.Bootstrap.Notification;

namespace Nameless.InfoPhoenix.Client.Tests.Views.Windows;

/// <summary>
/// Hand-rolled <see cref="IApplicationContext"/> fake. Only <see cref="ApplicationName"/>
/// and <see cref="Version"/> are read by <c>SplashScreenWindow</c>; the rest throw if ever
/// touched, so a test that accidentally depends on them fails loudly instead of silently
/// reading a meaningless default.
/// </summary>
internal sealed class FakeApplicationContext : IApplicationContext {
    public string ApplicationName { get; init; } = "InfoPhoenix.Tests";

    public string Version { get; init; } = "0.0.0-test";

    public string EnvironmentName => throw new NotSupportedException();

    public IFileManager FileManager => throw new NotSupportedException();

    public string GetEnvironmentVariable(string name) {
        throw new NotSupportedException();
    }
}

/// <summary>
/// Hand-rolled <see cref="IBootstrapper"/> fake. Each test supplies <see cref="OnRunAsync"/>
/// to script the exact progress/completion/failure/cancellation behaviour it needs.
/// </summary>
internal sealed class FakeBootstrapper : IBootstrapper {
    public required Func<IProgress<StepProgress>, CancellationToken, Task> OnRunAsync { get; init; }

    public Task RunAsync(IProgress<StepProgress> progress, CancellationToken cancellationToken) {
        return OnRunAsync(progress, cancellationToken);
    }
}