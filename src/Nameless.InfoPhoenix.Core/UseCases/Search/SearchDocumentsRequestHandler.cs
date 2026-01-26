using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Data;
using Nameless.InfoPhoenix.DTOs;
using Nameless.Lucene;
using Nameless.ObjectModel;
using Nameless.Validation;
using Nameless.WPF.Notifications;

namespace Nameless.InfoPhoenix.UseCases.Search;

public class SearchDocumentsRequestHandler : RequestHandlerBase<SearchDocumentsRequest, SearchDocumentsResponse> {
    private readonly IIndexProvider _indexProvider;

    public SearchDocumentsRequestHandler(
        AppDbContext appDbContext,
        INotificationService notificationService,
        IValidationService validationService,
        IIndexProvider indexProvider,
        ILogger<SearchDocumentsRequestHandler> logger)
    : base(
        appDbContext,
        notificationService,
        validationService,
        logger)
    { _indexProvider = indexProvider; }

    public override async Task<SearchDocumentsResponse> HandleAsync(SearchDocumentsRequest request, CancellationToken cancellationToken) {
        var validationResult = await ValidationService.ValidateAsync(request, cancellationToken);
        if (!validationResult.Success) {
            return validationResult.AsErrors();
        }

        try {
            var index = _indexProvider.Get(Constants.Index.Name);
            var tokens = ContentSearchTokensParser.Parse(request.Term);
            var queryDefinition = SearchDocumentsQueryDefinitionBuilder
                .Create(index)
                .UseDocumentDirectories(request.DocumentDirectories)
                .UseExactTerms(tokens.Exact)
                .UseIncludeTerms(tokens.Include)
                .UseExcludeTerms(tokens.Exclude)
                .UseSortBy(Constants.Index.Fields.DocumentDirectoryID)
                .UseSlice(request.Start, request.Limit)
                .Build();
            var searchDocumentsRequest = new LuceneSearchDocumentsRequest(
                query: queryDefinition.Query,
                sort: queryDefinition.Sort,
                start: queryDefinition.Start,
                limit: queryDefinition.Limit
            );

            var response = await index.SearchAsync(
                searchDocumentsRequest,
                cancellationToken
            ).SkipContextSync();

            return await response.Match(
                onSuccess: async value => {
                    var documentDirectories = await EnrichSearchResultAsync(
                        response.Value.Hits,
                        cancellationToken
                    ).SkipContextSync();

                    return new SearchDocumentsMetadata(
                        SearchTerm: request.Term,
                        HighlightTerms: [.. tokens.Exact, .. tokens.Include],
                        DocumentDirectories: documentDirectories,
                        Count: value.Count
                    );
                },
                onFailure: async errors => {
                    await NotificationService.NotifyFailureAsync(errors[0].Message)
                                             .SkipContextSync();

                    return SearchDocumentsMetadata.Empty;
                }
            );
        }
        catch (Exception ex) {
            Logger.SearchDocumentsFailure(request.Term, ex);

            

            return Error.Failure(ex.Message);
        }
    }

    private async Task<DocumentDirectoryDto[]> EnrichSearchResultAsync(ISearchHit[] hits, CancellationToken cancellationToken) {
        var groups = hits.GroupBy(hit => hit.GetString(Constants.Index.Fields.DocumentDirectoryID) ?? string.Empty);

        var result = new List<DocumentDirectoryDto>();
        foreach (var group in groups) {
            var documentDirectory = await AppDbContext.DocumentDirectories.SingleOrDefaultAsync(
                item => item.ID == Guid.Parse(group.Key),
                cancellationToken
            ) ?? throw new InvalidOperationException();

            var documentDirectoryDto = new DocumentDirectoryDto(documentDirectory) {
                Documents = [.. group.Select(item => item.ToDocumentDto())],
                DocumentCount = group.Count()
            };

            result.Add(documentDirectoryDto);
        }

        return [.. result];
    }
}