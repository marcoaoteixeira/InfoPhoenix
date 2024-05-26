using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.Lucene;

namespace Nameless.InfoPhoenix.Domain {
    public static class SearchHitExtension {
        #region Public Static Methods

        public static SearchResultEntryDto ToSearchResultEntry(this ISearchHit self)
            => new() {
                DocumentDirectoryID = Guid.Parse(self.GetString(nameof(SearchResultEntryDto.DocumentDirectoryID))),
                DocumentDirectoryLabel = self.GetString(nameof(SearchResultEntryDto.DocumentDirectoryLabel)),
                DocumentDirectoryPath = self.GetString(nameof(SearchResultEntryDto.DocumentDirectoryPath)),
                DocumentDirectoryLastIndexingTime = DateTime.Parse(self.GetString(nameof(SearchResultEntryDto.DocumentDirectoryLastIndexingTime))),

                DocumentID = Guid.Parse(self.DocumentID),
                DocumentFileName = self.GetString(nameof(SearchResultEntryDto.DocumentFileName)),
                DocumentFilePath = self.GetString(nameof(SearchResultEntryDto.DocumentFilePath)),
                DocumentContent = self.GetString(nameof(SearchResultEntryDto.DocumentContent)),
                DocumentLastIndexingTime = DateTime.Parse(self.GetString(nameof(SearchResultEntryDto.DocumentLastIndexingTime))),
            };

        #endregion
    }
}
