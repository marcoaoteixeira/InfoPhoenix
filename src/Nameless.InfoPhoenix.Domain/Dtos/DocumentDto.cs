namespace Nameless.InfoPhoenix.Domain.Dtos {
    public sealed record DocumentDto {
        #region Public Static Read-Only Properties

        public static DocumentDto Empty { get; } = new();

        #endregion

        #region Public Properties

        public Guid ID { get; init; }
        public string Content { get; init; } = string.Empty;
        public byte[] RawFile { get; init; } = [];
        public string FilePath { get; init; } = string.Empty;
        public DateTime? LastIndexingTime { get; init; }
        public DateTime? LastWriteTime { get; init; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }

        public string FileName => Path.GetFileNameWithoutExtension(FilePath);
        public bool Missing => !File.Exists(FilePath);

        public DocumentDirectoryDto DocumentDirectory { get; init; } = DocumentDirectoryDto.Empty;

        #endregion
    }
}
