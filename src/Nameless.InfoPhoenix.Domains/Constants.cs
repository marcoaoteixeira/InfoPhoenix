namespace Nameless.InfoPhoenix.Domains;

internal static class Constants {
    internal static class Common {
        internal static readonly string ConnStrPattern = "Data Source={0};Pooling=false";
        internal static readonly string DatabaseFileName = "database.db";
        internal static readonly string DatabaseBackupFolderName = "backup";
        internal static readonly string DatabaseBackupFileNamePattern = "info_phoenix_db_{0:yyyyMMddHHmmss}.bkp";
        internal static readonly string IndexName = "info_phoenix_index";

        internal const string DOUBLE_QUOTE = "\"";
        internal const string ASTERISK = "*";
    }

    internal static class Documents {
        internal static class Extensions {
            internal const string TXT = ".txt";
            internal const string DOC = ".doc";
            internal const string DOCX = ".docx";
            internal const string RTF = ".rtf";
            internal const string PDF = ".pdf";
            internal const string XPS = ".xps";
        }
    }

    internal static class Separators {
        internal const string WHITE_SPACE = " ";
    }
}