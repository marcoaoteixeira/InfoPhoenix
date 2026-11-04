using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;

namespace Nameless.InfoPhoenix.Client.Tests.Views.Windows;

/// <summary>
/// Runs WPF window component tests on a dedicated STA thread and rethrows whatever the test
/// body throws back on the calling (xUnit) thread, bounded by a timeout so a stuck WPF
/// pipeline can never hang the test run.
/// </summary>
internal static class StaWindowTestHarness {
    private static readonly MethodInfo OnContentRenderedMethod = typeof(Window).GetMethod(
        name: "OnContentRendered",
        bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance
    ) ?? throw new MissingMethodException(nameof(Window), "OnContentRendered");

    /// <summary>
    /// Runs <paramref name="body"/> on a new STA thread with its own <see cref="Dispatcher"/>,
    /// with that thread's <see cref="SynchronizationContext"/> set to a
    /// <see cref="DispatcherSynchronizationContext"/> for the thread's dispatcher - matching
    /// what WPF sets up for the real UI thread. Without this, <c>Progress&lt;T&gt;</c>
    /// (used by <c>SplashScreenWindow</c> to marshal bootstrap progress) finds
    /// <see cref="SynchronizationContext.Current"/> null and posts to the thread pool instead,
    /// bypassing the exact marshalling path production relies on.
    /// Every assertion a test needs to make on the window must happen inside
    /// <paramref name="body"/>: WPF objects are thread-affine, so asserting on them after
    /// this method returns (back on the xUnit thread) would throw.
    /// </summary>
    public static void Run(Action body, TimeSpan? timeout = null) {
        Exception? error = null;
        var thread = new Thread(() => {
            try {
                SynchronizationContext.SetSynchronizationContext(
                    new DispatcherSynchronizationContext(Dispatcher.CurrentDispatcher));

                body();
            }
            catch (Exception ex) {
                error = ex;
            }
            finally {
                // Without this, a `Dispatcher.Invoke`/`BeginInvoke` reaching this dispatcher
                // after the thread body returns (e.g. a `Progress<T>` callback landing late,
                // as in the "report after window closed" test) targets a dispatcher whose
                // thread already exited but was never shut down, and blocks the caller
                // forever. It also leaks HwndWrapper state per test.
                Dispatcher.CurrentDispatcher.InvokeShutdown();
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.IsBackground = true;
        thread.Start();

        var joined = thread.Join(timeout ?? TimeSpan.FromSeconds(10));
        if (!joined) {
            throw new TimeoutException("STA test thread did not complete within the allotted time.");
        }

        if (error is not null) {
            ExceptionDispatchInfo.Capture(error).Throw();
        }
    }

    /// <summary>
    /// Ensures <c>ContentRendered</c> fires deterministically for <paramref name="window"/>.
    /// On a host without an active, composited desktop session the OS signal that normally
    /// drives <c>ContentRendered</c> never arrives, even though layout and
    /// <see cref="Window.Loaded"/> complete normally; waiting on it would hang the test. Since
    /// this ticket's behaviour hangs off <c>ContentRendered</c> firing (not off an actual
    /// composited frame reaching the screen), this falls back to triggering the window's own
    /// protected <c>OnContentRendered</c> - driving the exact same production code path (the
    /// real, XAML-wired "SplashScreenReady" handler) - but only when the native signal does not
    /// arrive on its own. On a machine where it does fire natively, forcing it unconditionally
    /// would run `SplashScreenReady` (and therefore `Close()`) twice.
    /// </summary>
    public static void ArmContentRendered(Window window) {
        var fired = false;
        window.ContentRendered += (_, _) => fired = true;
        window.Loaded += (_, _) => window.Dispatcher.BeginInvoke(
            DispatcherPriority.ContextIdle,
            () => {
                if (!fired) {
                    OnContentRenderedMethod.Invoke(window, [EventArgs.Empty]);
                }
            });
    }
}
