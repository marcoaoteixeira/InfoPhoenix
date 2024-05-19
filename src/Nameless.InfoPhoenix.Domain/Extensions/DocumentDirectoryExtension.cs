using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Domain.Entities;

namespace Nameless.InfoPhoenix.Domain {
    public static class DocumentDirectoryExtension {
        #region Public Static Methods

        public static DocumentDirectoryDto ToDto(this DocumentDirectory? self)
            => self is not null
                ? new DocumentDirectoryDto {
                    ID = self.ID,
                    Label = self.Label,
                    DirectoryPath = self.DirectoryPath,
                    Order = self.Order,
                    LastIndexingTime = self.LastIndexingTime,
                    CreatedAt = self.CreatedAt,
                    ModifiedAt = self.ModifiedAt
                }
                : DocumentDirectoryDto.Empty;

        #endregion
    }
}
