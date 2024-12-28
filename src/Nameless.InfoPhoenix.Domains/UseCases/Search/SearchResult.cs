using Nameless.Search;

namespace Nameless.InfoPhoenix.Domains.UseCases.Search;

public sealed record SearchResult {
    public Guid DocumentDirectoryID { get; set; }
    public string DocumentDirectoryLabel { get; init; } = string.Empty;
    public string DocumentDirectoryPath { get; init; } = string.Empty;
    public int DocumentDirectoryOrder { get; init; }
    public DateTime DocumentDirectoryLastIndexingTime { get; init; }

    public Guid DocumentID { get; init; }
    public string DocumentFilePath { get; init; } = string.Empty;
    public string DocumentContent { get; init; } = string.Empty;
    public DateTime DocumentLastIndexingTime { get; init; }

    public static SearchResult From(ISearchHit searchHit)
        => new() {
            DocumentDirectoryID = Guid.Parse(searchHit.GetString(nameof(IndexFields.DocumentDirectoryID)) ?? string.Empty),
            DocumentDirectoryLabel = searchHit.GetString(nameof(IndexFields.DocumentDirectoryLabel)) ?? string.Empty,
            DocumentDirectoryPath = searchHit.GetString(nameof(IndexFields.DocumentDirectoryPath)) ?? string.Empty,
            DocumentDirectoryOrder = searchHit.GetInteger(nameof(IndexFields.DocumentDirectoryOrder)).GetValueOrDefault(),
            DocumentDirectoryLastIndexingTime = searchHit.GetDateTime(nameof(IndexFields.DocumentDirectoryLastIndexingTime)).GetValueOrDefault(),
            
            DocumentID = Guid.Parse(searchHit.GetString(nameof(IndexFields.DocumentID)) ?? string.Empty),
            DocumentFilePath = searchHit.GetString(nameof(IndexFields.DocumentFilePath)) ?? string.Empty,
            DocumentContent = searchHit.GetString(nameof(IndexFields.DocumentContent)) ?? string.Empty,
            DocumentLastIndexingTime = searchHit.GetDateTime(nameof(IndexFields.DocumentLastIndexingTime)).GetValueOrDefault(),
        };
}