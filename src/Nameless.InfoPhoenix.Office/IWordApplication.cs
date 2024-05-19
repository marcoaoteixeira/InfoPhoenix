namespace Nameless.InfoPhoenix.Office {
    public interface IWordApplication : IDisposable {
        #region Methods

        IWordDocument Open(string filePath);

        #endregion
    }
}
