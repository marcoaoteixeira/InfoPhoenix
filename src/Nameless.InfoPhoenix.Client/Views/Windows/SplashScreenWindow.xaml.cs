using System.Windows;
using Microsoft.Extensions.Configuration;
using Nameless.Application;
using Nameless.Configuration;
using Nameless.Contracts;
using Nameless.Windows.Bootstrap;
using Nameless.Windows.Bootstrap.Notification;
using Nameless.Windows.UI;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public interface ISplashScreenWindow : IWindow;

public partial class SplashScreenWindow : ISplashScreenWindow {
    private readonly IApplicationContext _applicationContext;
    private readonly IBootstrapper _bootstrapper;
    private readonly IConfiguration _configuration;
    private readonly IProgress<StepProgress> _progress;

    public SplashScreenWindow(IApplicationContext applicationContext, IBootstrapper bootstrapper, IConfiguration configuration) {
        _applicationContext = Throws.When.Null(applicationContext);
        _bootstrapper = Throws.When.Null(bootstrapper);
        _configuration = Throws.When.Null(configuration);
        _progress = new Progress<StepProgress>(StepProgressReporter);

        InitializeComponent();
        Initialize();
    }

    private void Initialize() {
        ApplicationNameTextBlock.Text = _applicationContext.ApplicationName;
        ApplicationVersionTextBlock.Text = _applicationContext.Version;
    }

    public void Show(WindowStartupLocation startupLocation) {
        WindowStartupLocation = startupLocation;

        ShowDialog();
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void SplashScreenReady(object? sender, EventArgs e) {
        var timeout = GetBootstrapTimeout();
        using var cts = new CancellationTokenSource(timeout);

        // Deliberately no ContinueWith/try-catch: a bootstrap failure (or
        // timeout) must propagate unhandled to the Dispatcher, not be
        // swallowed. See IPX-0003 plan deviation D1.
        await _bootstrapper.RunAsync(_progress, cts.Token);

        Close();
    }

    private void StepProgressReporter(StepProgress report) {
        Dispatcher.Invoke(() => {
            StepNameTextBlock.Text = report.StepName;
            StepMessageTextBlock.Text = report.Message;
        });
    }

    private int GetBootstrapTimeout() {
        return _configuration.GetSectionFor<BootstrapperOptions>()
                             .GetOrCreate<BootstrapperOptions>()
                             .WarmupTimeout;
    }
}