using Nameless.InfoPhoenix.Domains.Dtos;
using Nameless.InfoPhoenix.Domains.Entities;

namespace Nameless.InfoPhoenix.Domains;

internal static class DocumentDirectoryExtension {
    internal static DocumentDirectoryDto ToDto(this DocumentDirectory self)
        => ToDto(self, documentCount: 0);

    internal static DocumentDirectoryDto ToDto(this DocumentDirectory self, int documentCount)
        => new() {
            ID = self.ID,
            Label = self.Label,
            DirectoryPath = self.DirectoryPath,
            Order = self.Order,
            LastIndexingTime = self.LastIndexingTime,
            CreatedAt = self.CreatedAt,
            ModifiedAt = self.ModifiedAt,

            DocumentCount = documentCount
        };
}
