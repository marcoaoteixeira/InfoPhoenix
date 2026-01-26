using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Data;
using Nameless.Mediator.Requests;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases;

public abstract class RequestHandlerBase<TRequest, TResponse> : IRequestHandler<TRequest, TResponse>
    where TRequest : IRequest<TResponse> {
    protected AppDbContext AppDbContext { get; }
    
    protected INotificationService NotificationService { get; }
    
    protected IValidationService ValidationService { get; }
    
    protected ILogger Logger { get; }

    protected RequestHandlerBase(AppDbContext appDbContext, INotificationService notificationService, IValidationService validationService, ILogger logger) {
        AppDbContext = appDbContext;
        NotificationService = notificationService;
        ValidationService = validationService;
        Logger = logger;
    }

    public abstract Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken);
}