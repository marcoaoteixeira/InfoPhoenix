using MediatR;
using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Domains.Dtos;
using Nameless.InfoPhoenix.Domains.Entities;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.List;

public sealed class ListDocumentDirectoriesRequestHandler : IRequestHandler<ListDocumentDirectoriesRequest, DocumentDirectoryDto[]> {
    private readonly IRepository _repository;

    public ListDocumentDirectoriesRequestHandler(IRepository repository) {
        _repository = Prevent.Argument.Null(repository);
    }

    public Task<DocumentDirectoryDto[]> Handle(ListDocumentDirectoriesRequest request,
                                               CancellationToken cancellationToken) {
        var result = _repository.GetQuery<DocumentDirectory>()
                                .AsNoTracking()
                                .OrderBy(entity => entity.Order)
                                .Select(Map)
                                .ToArray();

        return Task.FromResult(result);
    }

    private int GetDocumentCount(Guid documentDirectoryId)
        => _repository.GetQuery<Document>()
                      .AsNoTracking()
                      .Count(document => document.DocumentDirectoryID == documentDirectoryId);

    private DocumentDirectoryDto Map(DocumentDirectory documentDirectory)
        => documentDirectory.ToDto(documentCount: GetDocumentCount(documentDirectory.ID));
}