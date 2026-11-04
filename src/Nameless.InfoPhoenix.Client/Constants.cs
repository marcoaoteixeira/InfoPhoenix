namespace Nameless.InfoPhoenix.Client;

internal static class Constants {
    internal static class Database {
        internal const string FileName = "app.db";
        internal const string ConnStrPattern = "Data Source={0};Pooling=false;";
    }

    internal static class Logging {
        internal const string Directory = "logs";
        internal const string FileNamePattern = "app-.log";
    }

    internal static class Application {
        internal const string Name = "InfoPhoenix";
    }
}
