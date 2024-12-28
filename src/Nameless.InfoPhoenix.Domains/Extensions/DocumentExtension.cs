using Nameless.InfoPhoenix.Domains.Dtos;
using Nameless.InfoPhoenix.Domains.Entities;

namespace Nameless.InfoPhoenix.Domains;

internal static class DocumentExtension {
    internal static DocumentDto ToDto(this Document self)
        => new() {
            ID = self.ID,
            DocumentDirectoryID = self.DocumentDirectoryID,
            FilePath = self.FilePath,
            Content = self.Content,
            RawFile = self.RawFile,
            LastIndexingTime = self.LastIndexingTime,
            LastWriteTime = self.LastWriteTime,
            CreatedAt = self.CreatedAt,
            ModifiedAt = self.ModifiedAt
        };
}
