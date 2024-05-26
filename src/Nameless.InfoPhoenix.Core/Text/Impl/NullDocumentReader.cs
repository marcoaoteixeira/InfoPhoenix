namespace Nameless.InfoPhoenix.Text.Impl {
    public sealed class NullDocumentReader : IDocumentReader {
        #region Public Static Read-Only Properties

        public static IDocumentReader Instance => new NullDocumentReader();

        #endregion

        #region Static Constructors

        // Explicit static constructor to tell C# compiler
        // not to mark type as beforefieldinit
        static NullDocumentReader() { }

        #endregion

        #region Private Constructors

        private NullDocumentReader() { }

        #endregion

        #region IDocumentReader Members

        public string[] Extensions => [];

        public string GetContent(string filePath) => string.Empty;

        #endregion
    }
}
