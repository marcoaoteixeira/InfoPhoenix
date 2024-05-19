namespace Nameless.InfoPhoenix.Office {
    public interface IWordDocument : IDisposable {
        #region Properties

        WordDocumentStatus Status { get; }

        #endregion

        #region Method

        string GetContent(bool formatted);

        void SaveAs(string filePath, DocumentType type);

        #endregion
    }
}
