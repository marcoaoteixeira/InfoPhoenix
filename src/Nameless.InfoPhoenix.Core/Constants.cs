using Nameless.InfoPhoenix.DTOs;

namespace Nameless.InfoPhoenix;

public static class Constants {
    public static class Application {
        public const string Name = "InfoPhoenix";
        public const string LogFileName = "application.log";

        public static class Backup {
            public const string DirectoryName = "backup";
            public const string FileNamePattern = "{0:yyyyMMddHHmmss}.app";
            public const string FileExtension = ".bkp";
        }

        public static class Temporary {
            public const string DirectoryName = "tmp";
        }
    }

    public static class Database {
        public const string DirectoryName = "database";
        public const string Name = "application.db";
        public const string ConnStrPattern = "Data Source={0};Pooling=false;";

        public static class Backup {
            public const string FileNamePattern = "{0:yyyyMMddHHmmss}.db";
        }
    }
    
    public static class Index {
        public const string DirectoryName = "index";
        public const string Name = "496E666F50686F656E6978202D204C7563656E6520496E646578";
        public const string SearchWildcard = "*";

        public static class Backup {
            public const string FileNamePattern = "{0:yyyyMMddHHmmss}.index";
        }

        public static class Fields {
            public const string DocumentDirectoryID = nameof(DocumentDto.DocumentDirectoryID);
            public const string DocumentID = nameof(DocumentDto.ID);
            public const string FilePath = nameof(DocumentDto.FilePath);
            public const string Content = nameof(DocumentDto.Content);
            public const string LastIndexingTime = nameof(DocumentDto.LastIndexingTime);
        }
    }

    public static class WellKnownDocuments {
        public static string[] AsArray { get; } = [Txt, Doc, Docx, Rtf, Pdf, Xps];

        public const string Txt = ".txt";
        public const string Doc = ".doc";
        public const string Docx = ".docx";
        public const string Rtf = ".rtf";
        public const string Pdf = ".pdf";
        public const string Xps = ".xps";
    }
}
