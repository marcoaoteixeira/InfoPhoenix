namespace Nameless.InfoPhoenix.Text {
    public interface IDocumentReaderProvider {
        #region Methods

        IDocumentReader GetDocumentReader(string extension);

        #endregion
    }
}
