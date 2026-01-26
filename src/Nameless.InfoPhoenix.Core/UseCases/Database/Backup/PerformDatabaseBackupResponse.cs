using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.Database.Backup;

public class PerformDatabaseBackupResponse : Result<PerformDatabaseBackupMetadata> {
    private PerformDatabaseBackupResponse(PerformDatabaseBackupMetadata value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator PerformDatabaseBackupResponse(PerformDatabaseBackupMetadata value) {
        return new PerformDatabaseBackupResponse(value, errors: []);
    }

    public static implicit operator PerformDatabaseBackupResponse(Error error) {
        return new PerformDatabaseBackupResponse(value: default, errors: [error]);
    }
}