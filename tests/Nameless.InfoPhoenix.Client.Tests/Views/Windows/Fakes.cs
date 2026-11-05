using Nameless.Windows.Bootstrap;
using Nameless.Windows.Bootstrap.Notification;

namespace Nameless.InfoPhoenix.Client.Tests.Views.Windows;

/// <summary>
/// Hand-rolled <see cref="IBootstrapper"/> fake. Each test supplies <see cref="OnRunAsync"/>
/// to script the exact progress/completion/failure/cancellation behaviour it needs.
/// </summary>
internal sealed class FakeBootstrapper : IBootstrapper {
    public required Func<IProgress<StepProgress>, CancellationToken, Task> OnRunAsync { get; init; }

    /// <summary>
    /// Number of times <see cref="RunAsync"/> was invoked. Guards against
    /// <c>SplashScreenReady</c> running more than once when the native
    /// <c>ContentRendered</c> signal and the harness's forced fallback both fire
    /// (see <see cref="StaWindowTestHarness.ArmContentRendered"/>).
    /// </summary>
    public int RunAsyncCallCount { get; private set; }

    public Task RunAsync(IProgress<StepProgress> progress, CancellationToken cancellationToken) {
        RunAsyncCallCount++;
        return OnRunAsync(progress, cancellationToken);
    }
}