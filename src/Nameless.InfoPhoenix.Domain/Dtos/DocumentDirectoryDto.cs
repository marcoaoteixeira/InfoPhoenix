namespace Nameless.InfoPhoenix.Domain.Dtos {
    public sealed record DocumentDirectoryDto {
        #region Public Static Read-Only Properties

        public static DocumentDirectoryDto Empty { get; } = new();

        #endregion

        #region Public Properties

        public Guid ID { get; init; }
        public string Label { get; init; } = string.Empty;
        public string DirectoryPath { get; init; } = string.Empty;
        public int Order { get; init; }
        public DateTime? LastIndexingTime { get; init; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }

        public string DirectoryName =>
            Path.GetFileName(DirectoryPath.RemoveTail([InfoPhoenix.Root.Defaults.DIRECTORY_SEPARATOR_CHAR]));

        public bool Missing => !Directory.Exists(DirectoryPath);

        public DocumentDto[] Documents { get; set; } = [];
        public int DocumentCount { get; set; }

        #endregion
    }
}
