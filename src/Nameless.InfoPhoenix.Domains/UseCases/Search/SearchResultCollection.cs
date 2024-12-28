using System.Collections;
using Nameless.InfoPhoenix.Domains.Dtos;

namespace Nameless.InfoPhoenix.Domains.UseCases.Search;

public sealed record SearchResultCollection : IEnumerable<SearchResult> {
    private HashSet<SearchResult> Entries { get; } = [];

    public SearchResultCollection() {
        
    }

    public SearchResultCollection(IEnumerable<SearchResult> collection) {
        Entries = [..collection];
    }

    public int Count => Entries.Count;

    public void Add(SearchResult item)
        => Entries.Add(item);

    public IEnumerable<DocumentDirectoryDto> GetDocumentDirectories()
        => Entries.GroupBy(item => item.DocumentDirectoryID)
                  .Select(group => new DocumentDirectoryDto {
                      ID = group.Key,
                      Label = group.First().DocumentDirectoryLabel,
                      DirectoryPath = group.First().DocumentDirectoryPath,
                      LastIndexingTime = group.First().DocumentDirectoryLastIndexingTime,
                      Order = group.First().DocumentDirectoryOrder,
                      DocumentCount = group.Count()
                  })
                  .OrderBy(item => item.Order);

    public IEnumerable<DocumentDto> GetDocuments(Guid documentDirectoryID)
        => Entries.Where(item => item.DocumentDirectoryID == documentDirectoryID)
                  .Select(item => new DocumentDto {
                      ID = item.DocumentID,
                      DocumentDirectoryID = item.DocumentDirectoryID,
                      FilePath = item.DocumentFilePath,
                      Content = item.DocumentContent,
                      LastIndexingTime = item.DocumentLastIndexingTime
                  });

    public IEnumerator<SearchResult> GetEnumerator()
        => Entries.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator()
        => Entries.GetEnumerator();
}