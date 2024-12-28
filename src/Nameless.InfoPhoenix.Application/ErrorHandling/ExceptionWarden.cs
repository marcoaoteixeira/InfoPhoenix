using System.Windows.Threading;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Notification;

namespace Nameless.InfoPhoenix.Application.ErrorHandling;

public sealed class ExceptionWarden : IExceptionWarden {
    private readonly INotificationService _notificationService;
    private readonly ILogger<ExceptionWarden> _logger;

    public ExceptionWarden(INotificationService notificationService,
                           ILogger<ExceptionWarden> logger) {
        _notificationService = Prevent.Argument.Null(notificationService);
        _logger = Prevent.Argument.Null(logger);
    }

    public void StartShift() {
        System.Windows.Application.Current.DispatcherUnhandledException += UnhandledExceptionHandler;
        Dispatcher.CurrentDispatcher.UnhandledException += UnhandledExceptionHandler;
        TaskScheduler.UnobservedTaskException += UnhandledExceptionHandler;
    }

    private void UnhandledExceptionHandler(object? _, UnobservedTaskExceptionEventArgs args) {
        ExceptionHandler(args.Exception);

        args.SetObserved();
    }

    private void UnhandledExceptionHandler(object _, DispatcherUnhandledExceptionEventArgs args) {
        ExceptionHandler(args.Exception);

        args.Handled = true;
    }

    private void ExceptionHandler(Exception exception) {
        _logger.LogError(exception, "Captured Unhandled Exception");

        _notificationService.PublishAsync(new ExceptionWardenNotification {
            Title = $"Erro: {exception.GetType().Name}",
            Message = exception.Message,
            Type = NotificationType.Error
        });
    }
}