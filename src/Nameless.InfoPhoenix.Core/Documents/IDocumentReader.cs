using Nameless.Result;

namespace Nameless.InfoPhoenix.Documents;

public interface IDocumentReader {
    bool CanRead(string extension);

    Result<string> GetContent(string filePath);
}