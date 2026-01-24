using Nameless.InfoPhoenix.Services;
using Nameless.InfoPhoenix.UseCases.Database.Backup;
using Nameless.InfoPhoenix.UseCases.Index.Backup;
using Nameless.IO.FileSystem;
using Nameless.Mediator;
using Nameless.Mediator.Requests;
using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.Application.Backup;

public readonly record struct PerformApplicationBackupMetadata(string BackupFilePath) {
    public bool IsBackupAvailable => !string.IsNullOrWhiteSpace(BackupFilePath);
}

public class PerformApplicationBackupResponse : Result<PerformApplicationBackupMetadata> {
    private PerformApplicationBackupResponse(PerformApplicationBackupMetadata value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator PerformApplicationBackupResponse(PerformApplicationBackupMetadata value) {
        return new PerformApplicationBackupResponse(value, errors: []);
    }

    public static implicit operator PerformApplicationBackupResponse(Error error) {
        return new PerformApplicationBackupResponse(value: default, errors: [error]);
    }
}

public class PerformApplicationBackupRequest : IRequest<PerformApplicationBackupResponse>;

public class PerformApplicationBackupRequestHandler : IRequestHandler<PerformApplicationBackupRequest, PerformApplicationBackupResponse> {
    private readonly IFileSystem _fileSystem;
    private readonly IMediator _mediator;
    private readonly IZipArchiveService _zipArchiveService;

    public PerformApplicationBackupRequestHandler(
        IFileSystem fileSystem,
        IMediator mediator,
        IZipArchiveService zipArchiveService) {
        _fileSystem = fileSystem;
        _mediator = mediator;
        _zipArchiveService = zipArchiveService;
    }

    public async Task<PerformApplicationBackupResponse> HandleAsync(PerformApplicationBackupRequest request, CancellationToken cancellationToken) {
        EnsureApplicationBackupDirectoryExistence();

        var databaseTask = _mediator.ExecuteAsync(
            new PerformDatabaseBackupRequest(),
            cancellationToken
        );
        var documentIndexTask = _mediator.ExecuteAsync(
            new PerformDocumentIndexBackupRequest(),
            cancellationToken
        );

        await Task.WhenAll(databaseTask, documentIndexTask)
                  .SkipContextSync();

        var databaseResponse = await databaseTask;
        var documentIndexResponse = await documentIndexTask;

        if (!databaseResponse.Success || !documentIndexResponse.Success) {
            return Error.Failure("");
        }

        // Here we combine the backup files into a single application backup file.
        var databaseBackupFile = databaseResponse.Value.BackupFilePath;
        var documentIndexBackupFile = documentIndexResponse.Value.BackupFilePath;

        return Error.Failure("");
    }

    private void EnsureApplicationBackupDirectoryExistence() {
        _fileSystem.GetDirectory(Constants.Application.Backup.DirectoryName)
                   .Create();
    }
}
