using Nameless.InfoPhoenix.Client.Data.Entities;

namespace Nameless.InfoPhoenix.Client.Data.DTOs;

internal static class Mappers {
    extension(DocumentDirectory self) {
        public DocumentDirectoryDto Map() {
            return new DocumentDirectoryDto {
                ID = self.ID,
                Label = self.Label,
                DirectoryPath = self.DirectoryPath,
                DisplayPosition = self.DisplayPosition,
                LastIndexingTime = self.LastIndexingTime,
                DocumentCount = self.Documents.Count
            };
        }
    }

    extension(Document self) {
        public DocumentDto Map() {
            return new DocumentDto {
                ID = self.ID,
                FilePath = self.FilePath,
                Content = self.Content,
                RawFile = self.RawFile,
                LastIndexingTime = self.LastIndexingTime,
                LastWriteTime = self.LastWriteTime,
                DocumentDirectoryID = self.DocumentDirectoryID,
            };
        }
    }
}
