using System.Collections;

namespace Nameless.InfoPhoenix.Domain.Dtos {
    public sealed record SearchResultCollectionDto : IEnumerable<SearchResultEntryDto> {
        #region Public Static Read-Only Properties

        public static SearchResultCollectionDto Empty { get; } = new(Array.Empty<SearchResultEntryDto>());

        #endregion

        #region Private Read-Only Fields

        private readonly SearchResultEntryDto[] _entries;

        #endregion

        #region Public Properties

        public SearchResultEntryDto this[int index] => _entries[index];
        public string DocumentDirectoryLabel { get; init; } = string.Empty;
        public string DocumentDirectoryPath { get; init; } = string.Empty;
        public DateTime DocumentDirectoryLastIndexingTime { get; init; }
        public int Count => _entries.Length;

        #endregion

        #region Public Constructors

        public SearchResultCollectionDto(SearchResultEntryDto[] entries) {
            _entries = entries;
        }

        #endregion

        #region IEnumerable<SearchResultEntryDto> Members
        
        public IEnumerator<SearchResultEntryDto> GetEnumerator()
            => ((IEnumerable<SearchResultEntryDto>)_entries).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => GetEnumerator();

        #endregion
    }
}
