using Nameless.ObjectModel;
using Nameless.Results;

namespace Nameless.InfoPhoenix.UseCases.Documents.ConvertXps;

public class ConvertIntoXpsFormatResponse : Result<ConvertIntoXpsFormatMetadata> {
    private ConvertIntoXpsFormatResponse(ConvertIntoXpsFormatMetadata value, Error[] errors)
        : base(value, errors) { }

    public static implicit operator ConvertIntoXpsFormatResponse(ConvertIntoXpsFormatMetadata value) {
        return new ConvertIntoXpsFormatResponse(value, errors: []);
    }

    public static implicit operator ConvertIntoXpsFormatResponse(Error error) {
        return new ConvertIntoXpsFormatResponse(value: default, errors: [error]);
    }
}