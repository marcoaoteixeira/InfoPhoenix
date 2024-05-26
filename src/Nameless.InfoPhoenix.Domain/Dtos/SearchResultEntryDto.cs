namespace Nameless.InfoPhoenix.Domain.Dtos {
    public sealed record SearchResultEntryDto {
        #region Public Static Read-Only Properties

        public static SearchResultEntryDto Empty { get; } = new();

        #endregion

        #region Public Properties

        public Guid DocumentDirectoryID { get; init; }
        public string DocumentDirectoryLabel { get; init; } = string.Empty;
        public string DocumentDirectoryPath { get; init; } = string.Empty;
        public DateTime DocumentDirectoryLastIndexingTime { get; init; }

        public Guid DocumentID { get; set; }
        public string DocumentFileName { get; set; } = string.Empty;
        public string DocumentFilePath { get; set; } = string.Empty;
        public string DocumentContent { get; set; } = string.Empty;
        public DateTime DocumentLastIndexingTime { get; set; }

        #endregion
    }
}
