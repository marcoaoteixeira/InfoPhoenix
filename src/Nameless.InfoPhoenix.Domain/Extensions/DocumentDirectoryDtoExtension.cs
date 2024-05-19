using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Domain.Entities;

namespace Nameless.InfoPhoenix.Domain {
    public static class DocumentDirectoryDtoExtension {
        #region Public Static Methods
        
        public static DocumentDirectory ToEntity(this DocumentDirectoryDto? self)
            => self is not null
                ? new DocumentDirectory {
                    ID = self.ID,
                    Label = self.Label,
                    DirectoryPath = self.DirectoryPath,
                    Order = self.Order,
                    LastIndexingTime = self.LastIndexingTime,
                    CreatedAt = self.CreatedAt,
                    ModifiedAt = self.ModifiedAt
                }
                : new DocumentDirectory();

        #endregion
    }
}
