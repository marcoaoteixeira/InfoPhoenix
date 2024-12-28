using MediatR;
using Microsoft.Extensions.Logging;
using Nameless.FileSystem;
using Nameless.InfoPhoenix.Documents;
using Nameless.InfoPhoenix.Notification;
using Nameless.Result;

namespace Nameless.InfoPhoenix.Domains.UseCases.Documents.Convert;

public sealed class ConvertDocumentToXpsFileRequestHandler : IRequestHandler<ConvertDocumentToXpsFileRequest, Result<string>> {
    private static readonly string[] AllowedDocumentExtensions = [
        Constants.Documents.Extensions.TXT,
        Constants.Documents.Extensions.DOC,
        Constants.Documents.Extensions.DOCX,
        Constants.Documents.Extensions.RTF
    ];

    private readonly IDocumentConverter _documentConverter;
    private readonly IFileSystem _fileSystem;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ConvertDocumentToXpsFileRequestHandler> _logger;

    public ConvertDocumentToXpsFileRequestHandler(IDocumentConverter documentConverter,
                                                  IFileSystem fileSystem,
                                                  INotificationService notificationService,
                                                  ILogger<ConvertDocumentToXpsFileRequestHandler> logger) {
        _documentConverter = Prevent.Argument.Null(documentConverter);
        _fileSystem = Prevent.Argument.Null(fileSystem);
        _notificationService = Prevent.Argument.Null(notificationService);
        _logger = Prevent.Argument.Null(logger);
    }

    public async Task<Result<string>> Handle(ConvertDocumentToXpsFileRequest request, CancellationToken cancellationToken) {
        request.Reporter.Report($"Convertendo arquivo: {_fileSystem.Path.GetFileName(request.FilePath)}");

        var extension = _fileSystem.Path.GetExtension(request.FilePath) ?? string.Empty;
        if (!AllowedDocumentExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase)) {
            return Error.Conflict("Não é possível converter o documento selecionado.");
        }

        var convertResult = _documentConverter.Convert(request.FilePath);
        if (convertResult.HasErrors) {
            await PushNotification("Não foi possível converter o arquivo", NotificationType.Error);

            return convertResult.Errors;
        }

        request.Reporter.Report("Criando arquivo temporário...");
        var tempFilePath = _fileSystem.Path.GetTempFileName();

        try {
            await using var stream = _fileSystem.File.Create(tempFilePath);
            await stream.WriteAsync(convertResult.Value, cancellationToken);
            await stream.FlushAsync(cancellationToken);
            stream.Close();
        } catch (Exception ex) {
            _logger.ErrorWhileWritingTempFile(ex);

            var error = Error.Failure($"Erro ao gravar o arquivo temporário. Error: {ex.Message}", exception: ex);

            await PushNotification(error.Description, NotificationType.Error);

            return error;
        }

        request.Reporter.Report("Arquivo temporário criado com sucesso.");

        return tempFilePath;
    }

    private Task PushNotification(string message, NotificationType type)
        => _notificationService.PublishAsync(new ConvertDocumentToXpsDocumentNotification {
            Title = "Converter Documento",
            Message = message,
            Type = type
        });
}