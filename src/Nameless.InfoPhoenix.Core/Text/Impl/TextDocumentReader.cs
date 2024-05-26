namespace Nameless.InfoPhoenix.Text.Impl {
    public sealed class TextDocumentReader : IDocumentReader {
        #region IDocumentReader Members

        public string[] Extensions => [".txt"];

        public string GetContent(string filePath)
            => File.ReadAllText(filePath);

        #endregion
    }
}
