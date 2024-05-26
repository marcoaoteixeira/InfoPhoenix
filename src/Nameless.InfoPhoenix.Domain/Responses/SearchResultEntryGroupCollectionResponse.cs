using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Responses;

namespace Nameless.InfoPhoenix.Domain.Responses {
    public sealed record SearchResultEntryGroupCollectionResponse : MultipleValueResponse<SearchResultCollectionDto> {
        #region Public Properties

        public string[] HighlightTerms { get; init; } = [];

        #endregion
    }
}
