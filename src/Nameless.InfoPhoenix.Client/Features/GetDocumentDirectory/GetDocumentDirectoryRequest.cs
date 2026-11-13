using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.Contracts;
using Nameless.InfoPhoenix.Client.Data;
using Nameless.InfoPhoenix.Client.Data.DTOs;
using Nameless.Mediator.Requests;
using Nameless.Windows.Events;
using Nameless.Windows.Localization;
using Nameless.Windows.Notification;
using Nameless.Windows.Notification.SnackBar;

namespace Nameless.InfoPhoenix.Client.Features.GetDocumentDirectory;

public class GetDocumentDirectoryRequest : IRequest<DocumentDirectoryDto> {
    public required Guid ID { get; init; }
}

public class GetDocumentDirectoryRequestHandler : IRequestHandler<GetDocumentDirectoryRequest, DocumentDirectoryDto> {
    private readonly AppDbContext _dbContext;
    private readonly IEventPublisher _eventPublisher;
    private readonly ITranslator<GetDocumentDirectoryRequestHandler> _translator;
    private readonly ILogger<GetDocumentDirectoryRequestHandler> _logger;

    public GetDocumentDirectoryRequestHandler(AppDbContext dbContext, IEventPublisher eventPublisher, ITranslator<GetDocumentDirectoryRequestHandler> translator, ILogger<GetDocumentDirectoryRequestHandler> logger) {
        _dbContext = Throws.When.Null(dbContext);
        _eventPublisher = Throws.When.Null(eventPublisher);
        _translator = Throws.When.Null(translator);
        _logger = Throws.When.Null(logger);
    }

    public async Task<DocumentDirectoryDto> HandleAsync(GetDocumentDirectoryRequest request, CancellationToken cancellationToken) {
        Throws.When.Null(request);

        var documentDirectory = await _dbContext.DocumentDirectories
                                                .SingleOrDefaultAsync(entity => entity.ID == request.ID, cancellationToken)
                                                .SkipContextSync();

        if (documentDirectory is not null) {
            return documentDirectory.Map();
        }

        await _eventPublisher.PublishWarningAsync<SnackBarNotificationEvent>(
            message: _translator[$"{nameof(HandleAsync)}_MissingDocumentDirectory", request.ID]
        );

        CommonLog.Warning(_logger, $"Missing Document Directory: {request.ID}");

        return DocumentDirectoryDto.Empty;
    }
}
