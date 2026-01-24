using Nameless.InfoPhoenix.Office;
using Nameless.Results;

namespace Nameless.InfoPhoenix.Documents;

/// <summary>
///     Defines a contract to deal with documents.
/// </summary>
public interface IDocumentService {
    /// <summary>
    ///     Retrieves the content of a document.
    /// </summary>
    /// <param name="filePath">
    ///     The file path to the document.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token.
    /// </param>
    /// <returns>
    ///     A <see cref="Task"/> representing the asynchronous action
    ///     execution. The result of the action is a <see cref="string"/>
    ///     representing the document content.
    /// </returns>
    Task<string> GetContentAsync(string filePath, CancellationToken cancellationToken);

    /// <summary>
    ///     Converts a document to the specified type.
    /// </summary>
    /// <param name="filePath">
    ///     The document file path.
    /// </param>
    /// <param name="output">
    ///     The target document type.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token.
    /// </param>
    /// <returns>
    ///     A <see cref="Task"/> representing the asynchronous action
    ///     where the result is a <see cref="Result{T}"/> containing
    ///     the path to the converted document.
    /// </returns>
    Task<Result<string>> ConvertAsync(string filePath, DocumentType output, CancellationToken cancellationToken);
}