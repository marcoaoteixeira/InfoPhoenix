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
    private static readonly object ResourceAssemblyGate = new();
    private static bool _resourceAssemblySet;

    private static readonly MethodInfo OnContentRenderedMethod = typeof(Window).GetMethod(
        name: "OnContentRendered",
        bindingAttr: BindingFlags.NonPublic | BindingFlags.Instance
    ) ?? throw new MissingMethodException(nameof(Window), "OnContentRendered");

    // `Application.ResourceAssembly`'s setter only allows ONE assignment ever, and treats a
    // prior *read* as that assignment too (it lazily defaults to Assembly.GetEntryAssembly()
    // on first get). The VSTest test host already reads it - with the entry assembly being
    // "testhost", not this app - before any of our code runs, so the public setter always
    // throws here. Writing the backing field directly is the only way left to point pack URI
    // resolution (`pack://application:,,,/...`) at the assembly that actually embeds our
    // resources (src/Nameless.InfoPhoenix.Client), as the production app relies on.
    private static readonly FieldInfo ResourceAssemblyField = typeof(System.Windows.Application).GetField(
        name: "_resourceAssembly",
        bindingAttr: BindingFlags.NonPublic | BindingFlags.Static
    ) ?? throw new MissingFieldException("Application", "_resourceAssembly");

    /// <summary>
    /// Runs <paramref name="body"/> on a new STA thread with its own <see cref="Dispatcher"/>.
    /// Every assertion a test needs to make on the window must happen inside
    /// <paramref name="body"/>: WPF objects are thread-affine, so asserting on them after
    /// this method returns (back on the xUnit thread) would throw.
    /// </summary>
    public static void Run(Action body, TimeSpan? timeout = null) {
        EnsureResourceAssembly();

        Exception? error = null;
        var thread = new Thread(() => {
            try {
                body();
            }
            catch (Exception ex) {
                error = ex;
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
    /// On a host without an active, composited desktop session (as observed for this test
    /// host) the OS signal that normally drives <c>ContentRendered</c> never arrives, even
    /// though layout and <see cref="Window.Loaded"/> complete normally; waiting on it would
    /// hang every test. Since this ticket's behaviour hangs off <c>ContentRendered</c> firing
    /// (not off an actual composited frame reaching the screen), triggering the window's own
    /// protected <c>OnContentRendered</c> right after <c>Loaded</c> drives the exact same
    /// production code path (the real, XAML-wired "SplashScreenReady" handler) deterministically.
    /// </summary>
    public static void ArmContentRendered(Window window) {
        window.Loaded += (_, _) => window.Dispatcher.BeginInvoke(() => OnContentRenderedMethod.Invoke(window, [EventArgs.Empty]));
    }

    private static void EnsureResourceAssembly() {
        lock (ResourceAssemblyGate) {
            if (_resourceAssemblySet) {
                return;
            }

            ResourceAssemblyField.SetValue(null, typeof(App).Assembly);
            _resourceAssemblySet = true;
        }
    }
}