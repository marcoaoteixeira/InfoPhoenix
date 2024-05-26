namespace Nameless.InfoPhoenix.Text {
    public interface IDocumentConverter {
        #region Methods

        byte[] Convert(string filePath);

        #endregion
    }
}
