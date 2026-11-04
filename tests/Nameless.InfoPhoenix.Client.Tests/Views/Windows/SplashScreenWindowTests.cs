using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Extensions.Configuration;
using Nameless.Configuration;
using Nameless.InfoPhoenix.Client.Views.Windows;
using Nameless.Windows.Bootstrap.Notification;

namespace Nameless.InfoPhoenix.Client.Tests.Views.Windows;

public class SplashScreenWindowTests {
    [Fact]
    public void Show_WithImmediateBootstrap_SetsCenterScreenAndIsModal() {
        StaWindowTestHarness.Run(() => {
            var runAsyncCompleted = false;
            var bootstrapper = new FakeBootstrapper {
                OnRunAsync = (_, _) => {
                    runAsyncCompleted = true;
                    return Task.CompletedTask;
                }
            };
            var window = CreateWindow(bootstrapper);

            window.Show(WindowStartupLocation.CenterScreen);

            // `Show` (ShowDialog, modal) only returns after Close() runs, which only
            // happens once the awaited RunAsync has completed.
            Assert.True(runAsyncCompleted);
            Assert.Equal(WindowStartupLocation.CenterScreen, window.WindowStartupLocation);
        });
    }

    [Fact]
    public void Show_WithStagedProgress_UpdatesStepTextsLiveAndApplicationFields() {
        StaWindowTestHarness.Run(() => {
            var applicationContext = new FakeApplicationContext {
                ApplicationName = "InfoPhoenix",
                Version = "9.9.9"
            };
            SplashScreenWindow? window = null;
            var bootstrapper = new FakeBootstrapper {
                OnRunAsync = async (progress, _) => {
                    progress.Report(new StepProgress { StepName = "S1", Message = "M1" });
                    await WaitForTextAsync(window!, "S1", "M1");

                    progress.Report(new StepProgress { StepName = "S2", Message = "M2" });
                    await WaitForTextAsync(window!, "S2", "M2");
                }
            };
            window = CreateWindow(bootstrapper, applicationContext: applicationContext);

            window.Show(WindowStartupLocation.CenterScreen);

            Assert.Equal("InfoPhoenix", GetText(window, "ApplicationNameTextBlock"));
            Assert.Equal("9.9.9", GetText(window, "ApplicationVersionTextBlock"));
        });
    }

    [Fact]
    public void Show_WithNoOpBootstrap_ClosesImmediately() {
        StaWindowTestHarness.Run(() => {
            var closed = false;
            var bootstrapper = new FakeBootstrapper { OnRunAsync = (_, _) => Task.CompletedTask };
            var window = CreateWindow(bootstrapper);
            window.Closed += (_, _) => closed = true;

            window.Show(WindowStartupLocation.CenterScreen);

            Assert.True(closed);
        });
    }

    [Fact]
    public void Show_WhenBootstrapThrows_PropagatesToDispatcherUnhandledException() {
        StaWindowTestHarness.Run(() => {
            var thrown = new InvalidOperationException("boom");
            Exception? observed = null;
            var bootstrapper = new FakeBootstrapper { OnRunAsync = (_, _) => Task.FromException(thrown) };
            var window = CreateWindow(bootstrapper);

            window.Dispatcher.UnhandledException += (_, e) => {
                observed = e.Exception;
                e.Handled = true;
                window.Close();
            };

            window.Show(WindowStartupLocation.CenterScreen);

            Assert.Same(thrown, observed);
        });
    }

    [Fact]
    public void Show_WhenWarmupTimeoutElapses_PropagatesOperationCanceledToDispatcherUnhandledException() {
        StaWindowTestHarness.Run(() => {
            var configuration = BuildConfiguration(("Bootstrapper:WarmupTimeout", "10"));
            Exception? observed = null;
            var bootstrapper = new FakeBootstrapper {
                OnRunAsync = async (_, token) => await Task.Delay(Timeout.Infinite, token)
            };
            var window = CreateWindow(bootstrapper, configuration);

            window.Dispatcher.UnhandledException += (_, e) => {
                observed = e.Exception;
                e.Handled = true;
                window.Close();
            };

            window.Show(WindowStartupLocation.CenterScreen);

            Assert.True(observed is OperationCanceledException, $"Expected an OperationCanceledException, got: {observed}");
        }, timeout: TimeSpan.FromSeconds(15));
    }

    [Fact]
    public void Report_AfterWindowClosed_DoesNotThrow() {
        StaWindowTestHarness.Run(() => {
            var closed = false;
            IProgress<StepProgress>? capturedProgress = null;
            var bootstrapper = new FakeBootstrapper {
                OnRunAsync = (progress, _) => {
                    capturedProgress = progress;
                    return Task.CompletedTask;
                }
            };
            var window = CreateWindow(bootstrapper);
            window.Closed += (_, _) => closed = true;

            Exception? unhandled = null;
            window.Dispatcher.UnhandledException += (_, e) => {
                unhandled = e.Exception;
                e.Handled = true;
            };

            window.Show(WindowStartupLocation.CenterScreen);

            Assert.True(closed);

            // Progress<T> posts its callback through the SynchronizationContext the harness
            // installed (the same DispatcherSynchronizationContext production uses), so the
            // call below only queues the post; pumping the dispatcher once afterwards is what
            // actually runs it against the already-closed window. Landing there must be
            // harmless; this test intentionally does not assert on the resulting text, only
            // that nothing thrown by the queued callback escapes unobserved.
            var error = Record.Exception(() => capturedProgress!.Report(new StepProgress { StepName = "Late", Message = "Late" }));
            window.Dispatcher.Invoke(() => { }, DispatcherPriority.Background);

            Assert.Null(error);
            Assert.Null(unhandled);
        });
    }

    [Fact]
    public void GetBootstrapTimeout_FromEmptyConfiguration_DoesNotCancelImmediately() {
        var configuration = new ConfigurationBuilder().Build();

        var warmupTimeout = configuration
            .GetSectionFor<Nameless.Windows.Bootstrap.BootstrapperOptions>()
            .GetOrCreate<Nameless.Windows.Bootstrap.BootstrapperOptions>()
            .WarmupTimeout;

        // CancellationTokenSource treats Timeout.Infinite (-1) as "never cancel", which is
        // safe. Anything else must be a generous, non-immediate timeout. This guards against
        // the package default regressing to a value (e.g. 0) that cancels the bootstrap
        // before it can run.
        Assert.True(
            warmupTimeout == Timeout.Infinite || warmupTimeout >= 1000,
            $"WarmupTimeout default ({warmupTimeout}) would cancel the bootstrap run immediately."
        );
    }

    [Fact]
    public void Show_WithNullStepProgressFields_DoesNotThrowAndKeepsUpdatingAfterwards() {
        StaWindowTestHarness.Run(() => {
            SplashScreenWindow? window = null;
            var bootstrapper = new FakeBootstrapper {
                OnRunAsync = async (progress, _) => {
                    progress.Report(new StepProgress { StepName = null!, Message = null! });

                    progress.Report(new StepProgress { StepName = "S2", Message = "M2" });
                    await WaitForTextAsync(window!, "S2", "M2");
                }
            };
            window = CreateWindow(bootstrapper);

            window.Show(WindowStartupLocation.CenterScreen);

            Assert.Equal("S2", GetText(window, "StepNameTextBlock"));
            Assert.Equal("M2", GetText(window, "StepMessageTextBlock"));
        });
    }

    private static SplashScreenWindow CreateWindow(
        FakeBootstrapper bootstrapper,
        IConfiguration? configuration = null,
        FakeApplicationContext? applicationContext = null) {
        var window = new SplashScreenWindow(
            applicationContext ?? new FakeApplicationContext(),
            bootstrapper,
            configuration ?? new ConfigurationBuilder().Build()
        );

        StaWindowTestHarness.ArmContentRendered(window);

        return window;
    }

    private static IConfiguration BuildConfiguration(params (string Key, string Value)[] entries) {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e => new KeyValuePair<string, string?>(e.Key, e.Value)))
            .Build();
    }

    private static string? GetText(Window window, string elementName) {
        return ((TextBlock)window.FindName(elementName)!).Text;
    }

    private static async Task WaitForTextAsync(Window window, string stepName, string message) {
        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(5);

        while (DateTime.UtcNow < deadline) {
            var (actualStep, actualMessage) = window.Dispatcher.Invoke(() => (
                GetText(window, "StepNameTextBlock"),
                GetText(window, "StepMessageTextBlock")
            ));

            if (actualStep == stepName && actualMessage == message) {
                return;
            }

            await Task.Delay(20);
        }

        throw new TimeoutException($"Timed out waiting for StepName='{stepName}' Message='{message}'.");
    }
}