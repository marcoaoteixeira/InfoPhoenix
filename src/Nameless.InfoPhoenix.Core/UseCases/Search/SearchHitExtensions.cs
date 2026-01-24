using Nameless.InfoPhoenix.DTOs;
using Nameless.Lucene;

namespace Nameless.InfoPhoenix.UseCases.Search;

internal static class SearchHitExtensions {
    extension(ISearchHit self) {
        internal DocumentDto ToDocumentDto() {
            return new DocumentDto {
                ID = Guid.TryParse(self.GetString(Constants.Index.Fields.DocumentID), out var documentID) ? documentID : Guid.Empty,
                DocumentDirectoryID = Guid.TryParse(self.GetString(Constants.Index.Fields.DocumentDirectoryID), out var documentDirectoryID) ? documentDirectoryID : Guid.Empty,
                FilePath = self.GetString(Constants.Index.Fields.FilePath) ?? string.Empty,
                Content = self.GetString(Constants.Index.Fields.Content) ?? string.Empty,
                LastIndexingTime = self.GetDateTime(Constants.Index.Fields.LastIndexingTime)
            };
        }
    }
}