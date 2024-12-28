using MediatR;
using Microsoft.EntityFrameworkCore;
using Nameless.InfoPhoenix.Domains.Dtos;
using Nameless.InfoPhoenix.Domains.Entities;

namespace Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Get;

public sealed class GetDocumentDirectoryRequestHandler : IRequestHandler<GetDocumentDirectoryRequest, DocumentDirectoryDto> {
    private readonly IRepository _repository;

    public GetDocumentDirectoryRequestHandler(IRepository repository) {
        _repository = Prevent.Argument.Null(repository);
    }

    public async Task<DocumentDirectoryDto> Handle(GetDocumentDirectoryRequest request,
                                                   CancellationToken cancellationToken) {
        var currentDocumentDirectory = await _repository.GetQuery<DocumentDirectory>()
                                                        .AsNoTracking()
                                                        .SingleOrDefaultAsync(documentDirectory => documentDirectory.ID == request.ID,
                                                                              cancellationToken);

        var result = currentDocumentDirectory is not null
            ? currentDocumentDirectory.ToDto()
            : DocumentDirectoryDto.Empty;

        return result;
    }
}