using MediatR;
using Nameless.InfoPhoenix.Domain.Responses;

namespace Nameless.InfoPhoenix.Domain.Requests {
    public sealed record ExecuteSearchRequest : IRequest<SearchResultEntryGroupCollectionResponse> {
        #region Public Properties

        public string Query { get; init; } = string.Empty;

        #endregion
    }
}
