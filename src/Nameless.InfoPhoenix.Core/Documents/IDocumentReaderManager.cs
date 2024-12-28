using Nameless.Result;

namespace Nameless.InfoPhoenix.Documents;

public interface IDocumentReaderManager {
    Result<string> GetDocumentContent(string filePath);
}