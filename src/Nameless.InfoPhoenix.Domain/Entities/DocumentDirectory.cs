using System.Diagnostics;

namespace Nameless.InfoPhoenix.Domain.Entities {
    [DebuggerDisplay("{DebuggerDisplay}")]
    public sealed class DocumentDirectory : EntityBase {
        #region Private Properties

        private string DebuggerDisplay
            => $"ID: {ID}, Label: {Label}, DirectoryPath: {DirectoryPath}";

        #endregion

        #region Public Properties

        public string Label { get; set; } = string.Empty;
        public string DirectoryPath { get; set; } = string.Empty;
        public int Order { get; set; }
        public DateTime? LastIndexingTime { get; set; }

        #endregion
    }
}
