using Nameless.Result;

namespace Nameless.InfoPhoenix.Documents;

public interface IDocumentConverter {
    Result<byte[]> Convert(string filePath);
}