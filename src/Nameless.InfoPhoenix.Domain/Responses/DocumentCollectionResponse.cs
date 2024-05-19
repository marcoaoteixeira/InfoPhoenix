using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Responses;

namespace Nameless.InfoPhoenix.Domain.Responses {
    public sealed record DocumentCollectionResponse : MultipleValueResponse<DocumentDto> {
    }
}
