namespace Nameless.InfoPhoenix.Office {
    public interface IOfficeSuite {
        #region Methods

        string GetWordDocumentContent(string filePath, bool formatted);

        #endregion
    }
}
