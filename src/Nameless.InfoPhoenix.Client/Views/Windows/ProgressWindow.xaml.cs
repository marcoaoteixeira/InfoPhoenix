using System.Windows;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public sealed record ExecutionResult {
    public static ExecutionResult Empty => new();

    public bool AutoClose { get; init; }
}

public delegate Task<ExecutionResult> ExecuteAsync(IProgress<string> progress, CancellationToken cancellationToken);

public partial class ProgressWindow : IDisposable {
    private readonly Progress<string> _progress;

    private ExecuteAsync? _executeAsync;
    private CancellationTokenSource? _cts;

    private bool _disposed;

    public ProgressWindow() {
        _progress = new Progress<string>(UpdateProgressNotification);

        InitializeComponent();
    }

    ~ProgressWindow() {
        Dispose(disposing: false);
    }

    public void SetTitle(string title)
        => TitleTextBlock.Text = title;

    public void SetExecuteAction(ExecuteAsync executeAsync)
        => _executeAsync = Prevent.Argument.Null(executeAsync);

    public void Dispose() {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    private void UpdateProgressNotification(string state)
        => Dispatcher.InvokeAsync(() => ProgressNotificationTextBlock.Text = state);

    private void StartupHandler(object _, RoutedEventArgs __) {
        var cancellationToken = GetCancellationTokenSource().Token;

        try {
            Task.Run(async () => {
                ToggleControlsState(ProgressState.Running);

                var result = await GetExecuteAsync().Invoke(_progress, cancellationToken);

                ToggleControlsState(ProgressState.Idle);

                if (result.AutoClose) {
                    ForceClose();
                }
            }, cancellationToken);
        }
        catch { ToggleControlsState(ProgressState.Idle); }
    }

    private void Dispose(bool disposing) {
        if (_disposed) {
            return;
        }

        if (disposing) {
            _cts?.Dispose();
        }

        _cts = null;
        _disposed = true;
    }

    private ExecuteAsync GetExecuteAsync()
        => _executeAsync ??= (_, _) => Task.FromResult(ExecutionResult.Empty);

    private CancellationTokenSource GetCancellationTokenSource()
        => _cts ??= new CancellationTokenSource();

    private void CancelHandler(object _, RoutedEventArgs __)
        => GetCancellationTokenSource().Cancel();

    private void CloseHandler(object _, RoutedEventArgs __) {
        DialogResult = true;

        Close();
    }

    private void ToggleControlsState(ProgressState state)
        => Dispatcher.InvokeAsync(() => {
            MainProgressRing.IsIndeterminate = state == ProgressState.Running;
            CancelButton.IsEnabled = state == ProgressState.Running;
            CloseButton.IsEnabled = state == ProgressState.Idle;
        });

    private void ForceClose()
        => Dispatcher.InvokeAsync(() => {
            DialogResult = true;
            Close();
        });
}

public enum ProgressState {
    Idle,

    Running,
}

public static class ProgressRunnerHelper {
    public static void Run(string title, ExecuteAsync execute) {
        using var progressWindow = new ProgressWindow();

        progressWindow.Owner = System.Windows.Application.Current.MainWindow;
        progressWindow.SetTitle(title);
        progressWindow.SetExecuteAction(execute);

        progressWindow.ShowDialog();
    }
}