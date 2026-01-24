namespace Nameless.InfoPhoenix.Office;

/// <summary>
///     Defines the Word application interface.
/// </summary>
public interface IWordApplication : IDisposable {
    /// <summary>
    ///     Opens a Word document from the specified file path.
    /// </summary>
    /// <param name="filePath">
    ///     The file path of the Word document to open.
    /// </param>
    /// <returns>
    ///     An instance of <see cref="IWordDocument"/> representing
    ///     the opened document.
    /// </returns>
    IWordDocument Open(string filePath);
}