using System.Windows;
using Microsoft.Extensions.Configuration;
using Nameless.Bootstrap;
using Nameless.Infrastructure;
using Nameless.WPF.Bootstrap;
using Nameless.WPF.Windows;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class SplashScreenWindow : ISplashScreenWindow {
    private readonly IApplicationContext _applicationContext;
    private readonly IBootstrapper _bootstrapper;
    private readonly IConfiguration _configuration;
    private readonly IProgress<StepReport> _progress;

    public SplashScreenWindow(
        IApplicationContext applicationContext,
        IBootstrapper bootstrapper,
        IConfiguration configuration) {
        _applicationContext = applicationContext;
        _bootstrapper = bootstrapper;
        _configuration = configuration;

        _progress = new Progress<StepReport>(UpdateControls);

        InitializeComponent();
        Initialize();
    }

    public void Show(WindowStartupLocation startupLocation) {
        WindowStartupLocation = startupLocation;

        ShowDialog();
    }

    private void Initialize() {
        ApplicationNameTextBlock.Text = _applicationContext.ApplicationName;
        ApplicationVersionTextBlock.Text = _applicationContext.Version;
    }

    // ReSharper disable once AsyncVoidEventHandlerMethod
    private async void SplashScreenReady(object? sender, EventArgs e) {
        var flowContext = new FlowContext().SetStepProgress(_progress);

        var timeout = GetBootstrapTimeout();
        using var cts = new CancellationTokenSource(timeout);

        await _bootstrapper.ExecuteAsync(flowContext, cts.Token)
                           .ContinueWith(_ => Dispatcher.Invoke(Close), cts.Token)
                           .SkipContextSync();
    }

    private void UpdateControls(StepReport report) {
        Dispatcher.Invoke(() => {
            StepNameTextBlock.Text = report.Title;
            StepMessageTextBlock.Text = report.Message;
        });
    }

    private int GetBootstrapTimeout() {
        var value = _configuration.GetSection(nameof(Bootstrapper))
                                  .Get<int>();

        return value > 0 ? value : -1;
    }
}

public interface ISplashScreenWindow : IWindow;