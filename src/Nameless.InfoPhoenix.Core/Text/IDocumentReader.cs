namespace Nameless.InfoPhoenix.Text {
    public interface IDocumentReader {
        #region Properties

        string[] Extensions { get; }

        #endregion

        #region Methods

        string GetContent(string filePath);

        #endregion
    }
}
