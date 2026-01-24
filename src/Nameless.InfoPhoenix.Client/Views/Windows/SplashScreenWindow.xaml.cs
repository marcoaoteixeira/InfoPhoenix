using System.Windows;
using Nameless.Infrastructure;
using Nameless.WPF.Bootstrap;
using Nameless.WPF.Windows;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class SplashScreenWindow : ISplashScreenWindow {
    private readonly IApplicationContext _applicationContext;
    private readonly IBootstrapper _bootstrapper;
    private readonly IProgress<BootstrapperProgressReport> _progress;

    public SplashScreenWindow(IApplicationContext applicationContext, IBootstrapper bootstrapper) {
        _applicationContext = applicationContext;
        _bootstrapper = bootstrapper;
        _progress = new Progress<BootstrapperProgressReport>(UpdateControls);

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

    // ReSharper disable once AsyncVoidMethod
    private async void SplashScreenReady(object? sender, EventArgs e) {
        _bootstrapper.SetProgress(_progress);

        await _bootstrapper.ExecuteAsync(CancellationToken.None)
                           .ContinueWith(_ => Dispatcher.Invoke(Close))
                           .SkipContextSync();
    }

    private void UpdateControls(BootstrapperProgressReport report) {
        Dispatcher.Invoke(() => {
            StepNameTextBlock.Text = report.Title;
            StepMessageTextBlock.Text = report.Message;
        });
    }
}

public interface ISplashScreenWindow : IWindow;