using System.Diagnostics;

namespace Nameless.InfoPhoenix.Domain.Entities {
    [DebuggerDisplay("{DebuggerDisplay}")]
    public sealed class Document : EntityBase {
        #region Private Properties

        private string DebuggerDisplay
            => $"ID:{ID}, DocumentDirectoryID:{DocumentDirectoryID}, FilePath:{FilePath}";

        #endregion

        #region Public Properties

        public Guid DocumentDirectoryID { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public byte[] RawFile { get; set; } = [];
        public DateTime? LastIndexingTime { get; set; }
        public DateTime? LastWriteTime { get; set; }

        #endregion
    }
}
