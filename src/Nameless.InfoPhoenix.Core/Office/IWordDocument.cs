namespace Nameless.InfoPhoenix.Office;

public interface IWordDocument : IDisposable {
    WordDocumentStatus Status { get; }

    string GetContent(bool formatted);

    void SaveAs(string filePath, DocumentType type);
}