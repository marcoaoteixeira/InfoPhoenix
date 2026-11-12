using Microsoft.EntityFrameworkCore;
using Nameless.Contracts;
using Nameless.InfoPhoenix.Client.Data;
using Nameless.InfoPhoenix.Client.Data.DTOs;
using Nameless.Mediator.Requests;

namespace Nameless.InfoPhoenix.Client.Features.FetchDocumentDirectories;

public class FetchDocumentDirectoriesRequestHandler : IRequestHandler<FetchDocumentDirectoriesRequest, DocumentDirectoryDto[]> {
    private readonly AppDbContext _dbContext;

    public FetchDocumentDirectoriesRequestHandler(AppDbContext dbContext) {
        _dbContext = Throws.When.Null(dbContext);
    }

    public Task<DocumentDirectoryDto[]> HandleAsync(FetchDocumentDirectoriesRequest request, CancellationToken cancellationToken) {
        var documentDirectories = _dbContext.DocumentDirectories
                                            .AsNoTracking()
                                            .Select(item => item.Map())
                                            .ToArray();

        return Task.FromResult(documentDirectories);
    }
}