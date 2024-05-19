using Nameless.InfoPhoenix.Client.Models;
using Nameless.InfoPhoenix.Domain.Dtos;

namespace Nameless.InfoPhoenix.Client.Extensions {
    public static class DocumentDirectoryDtoExtension {
        #region Public Static Methods

        public static DocumentDirectoryModel ToModel(this DocumentDirectoryDto? self)
            => self is not null
                ? new DocumentDirectoryModel {
                    ID = self.ID,
                    Label = self.Label,
                    DirectoryPath = self.DirectoryPath,
                    Order = self.Order,
                    LastIndexingTime = self.LastIndexingTime,
                }
                : new DocumentDirectoryModel();

        public static DocumentDirectoryDto ToDto(this DocumentDirectoryModel? self)
            => self is not null
                ? new DocumentDirectoryDto {
                    ID = self.ID,
                    Label = self.Label,
                    DirectoryPath = self.DirectoryPath,
                    Order = self.Order,
                    LastIndexingTime = self.LastIndexingTime,
                }
                : DocumentDirectoryDto.Empty;

        #endregion
    }
}
