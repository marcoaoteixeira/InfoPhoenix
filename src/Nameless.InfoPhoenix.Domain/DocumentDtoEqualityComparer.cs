using Nameless.InfoPhoenix.Domain.Dtos;

namespace Nameless.InfoPhoenix.Domain {
    public sealed class DocumentDtoEqualityComparer : IEqualityComparer<DocumentDto> {
        #region Public Static Read-Only Properties

        public static DocumentDtoEqualityComparer Default { get; } = new();

        #endregion

        #region IEqualityComparer<DocumentDto> Members

        public bool Equals(DocumentDto? left, DocumentDto? right) {
            if (left is null || right is null) {
                return false;
            }

            return string.Equals(left.FilePath, right.FilePath, StringComparison.InvariantCultureIgnoreCase) &&
                   left.LastWriteTime == right.LastWriteTime &&
                   string.Equals(left.DocumentDirectory.DirectoryPath, right.DocumentDirectory.DirectoryPath, StringComparison.InvariantCultureIgnoreCase);
        }

        public int GetHashCode(DocumentDto obj)
            => HashCode.Combine(obj.FilePath.ToLowerInvariant(),
                                obj.LastWriteTime,
                                obj.DocumentDirectory.DirectoryPath.ToLowerInvariant());

        #endregion
    }
}
