using Nameless.InfoPhoenix.Entities;

namespace Nameless.InfoPhoenix.Data.Configurations;

/// <summary>
///     Naming conventions for table/entities.
/// </summary>
internal static class Naming {
    internal static class Documents {
        internal const string Table = nameof(Documents);

        internal static class Columns {
            internal const string ID = nameof(Document.ID);
            internal const string FilePath = nameof(Document.FilePath);
            internal const string Content = nameof(Document.Content);
            internal const string RawFile = nameof(Document.RawFile);
            internal const string LastIndexingTime = nameof(Document.LastIndexingTime);
            internal const string LastWriteTime = nameof(Document.LastWriteTime);
            internal const string DocumentDirectoryID = $"{nameof(DocumentDirectory)}{nameof(DocumentDirectory.ID)}";
            internal const string CreatedAt = nameof(Document.CreatedAt);
            internal const string ModifiedAt = nameof(Document.ModifiedAt);
        }

        internal static class Constraints {
            internal const string Pk = $"PK_{Table}_{nameof(Document.ID)}";
        }

        internal static class Indexes {
            internal const string ID = $"IDX_{Table}_{nameof(Document.ID)}";
            internal const string FilePath = $"IDX_{Table}_{nameof(Document.FilePath)}";
            internal const string LastIndexingTime = $"IDX_{Table}_{nameof(Document.LastIndexingTime)}";
        }
    }

    internal static class DocumentDirectories {
        internal const string Table = nameof(DocumentDirectories);

        internal static class Columns {
            internal const string ID = nameof(DocumentDirectory.ID);
            internal const string Label = nameof(DocumentDirectory.Label);
            internal const string DirectoryPath = nameof(DocumentDirectory.DirectoryPath);
            internal const string DisplayPosition = nameof(DocumentDirectory.DisplayPosition);
            internal const string LastIndexingTime = nameof(DocumentDirectory.LastIndexingTime);
            internal const string CreatedAt = nameof(DocumentDirectory.CreatedAt);
            internal const string ModifiedAt = nameof(DocumentDirectory.ModifiedAt);
        }

        internal static class Constraints {
            internal const string Pk = $"PK_{Table}_{nameof(DocumentDirectory.ID)}";
        }

        internal static class Indexes {
            internal const string ID = $"IDX_{Table}_{nameof(DocumentDirectory.ID)}";
            internal const string DirectoryPath = $"IDX_{Table}_{nameof(DocumentDirectory.DirectoryPath)}";
        }
    }
}
