using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Nameless.InfoPhoenix.Text.Impl {
    public sealed class DocumentReaderProvider : IDocumentReaderProvider {
        #region Private Read-Only Fields

        private readonly Dictionary<string, HashSet<IDocumentReader>> _cache = [];
        private readonly ILogger _logger;

        #endregion

        #region Public Constructors

        public DocumentReaderProvider(IServiceProvider serviceProvider) {
            Guard.Against.Null(serviceProvider, nameof(serviceProvider));

            _logger = serviceProvider.GetLogger<DocumentReaderProvider>();

            var documentReaders = serviceProvider
                .GetRequiredService<IEnumerable<IDocumentReader>>()
                .ToArray();

            var extensions = documentReaders
                .SelectMany(item => item.Extensions)
                .Distinct();

            foreach (var extension in extensions) {
                var documentReadersByExtension = documentReaders
                    .Where(item => item.Extensions.Contains(extension));

                _cache[extension] = [.. documentReadersByExtension];
            }
        }

        #endregion

        #region IDocumentReaderProvider Members

        public IDocumentReader GetDocumentReader(string extension) {
            if (_cache.TryGetValue(extension, out var documentReaders)) {
                return documentReaders.First();
            }

            _logger.LogWarning("Document reader for extension {Extension} not found.", extension);

            return NullDocumentReader.Instance;
        }

        #endregion
    }
}
