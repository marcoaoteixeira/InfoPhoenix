using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.Index.Backup;

public class PerformDocumentIndexBackupResponse : Result<PerformDocumentIndexBackupMetadata> {
    private PerformDocumentIndexBackupResponse(PerformDocumentIndexBackupMetadata value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator PerformDocumentIndexBackupResponse(PerformDocumentIndexBackupMetadata value) {
        return new PerformDocumentIndexBackupResponse(value, errors: []);
    }

    public static implicit operator PerformDocumentIndexBackupResponse(Error error) {
        return new PerformDocumentIndexBackupResponse(value: default, errors: [error]);
    }
}