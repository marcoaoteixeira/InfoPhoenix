using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Domain.Entities;
using Nameless.Lucene;

namespace Nameless.InfoPhoenix.Domain {
    public static class DocumentExtension {
        #region Public Static Methods

        public static DocumentDto ToDto(this Document self, DocumentDirectoryDto? documentDirectoryDto = null)
            => new() {
                ID = self.ID,
                FilePath = self.FilePath,
                Content = self.Content,
                RawFile = self.RawFile,
                LastIndexingTime = self.LastIndexingTime,
                LastWriteTime = self.LastWriteTime,
                CreatedAt = self.CreatedAt,
                ModifiedAt = self.ModifiedAt,
                DocumentDirectory = documentDirectoryDto ?? DocumentDirectoryDto.Empty
            };

        public static IDocument ToIndexDocument(this Document self, IIndex index, DocumentDirectory documentDirectory)
            => index.NewDocument(self.ID.ToString())
                    .Set(nameof(SearchResultEntryDto.DocumentDirectoryID), documentDirectory.ID.ToString(), FieldOptions.Store)
                    .Set(nameof(SearchResultEntryDto.DocumentDirectoryLabel), documentDirectory.Label, FieldOptions.Store)
                    .Set(nameof(SearchResultEntryDto.DocumentDirectoryPath), documentDirectory.DirectoryPath, FieldOptions.Store)
                    .Set(nameof(SearchResultEntryDto.DocumentDirectoryLastIndexingTime), documentDirectory.LastIndexingTime.GetValueOrDefault().ToString("G"), FieldOptions.Store)
                    .Set(nameof(SearchResultEntryDto.DocumentFileName), Path.GetFileNameWithoutExtension(self.FilePath), FieldOptions.Store)
                    .Set(nameof(SearchResultEntryDto.DocumentFilePath), self.FilePath, FieldOptions.Store)
                    .Set(nameof(SearchResultEntryDto.DocumentContent), self.Content, FieldOptions.Store | FieldOptions.Analyze)
                    .Set(nameof(SearchResultEntryDto.DocumentLastIndexingTime), self.LastIndexingTime.GetValueOrDefault().ToString("G"), FieldOptions.Store)
                    .Set(nameof(SearchResultEntryDto.DocumentLastWriteTime), self.LastWriteTime.GetValueOrDefault().ToString("G"), FieldOptions.Store);

        #endregion
    }
}
