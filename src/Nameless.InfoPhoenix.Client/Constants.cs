namespace Nameless.InfoPhoenix.Client;

internal static class Constants {
    internal static class Database {
        internal const string FileName = "app.db";
        internal const string ConnStrPattern = "Data Source={0};Pooling=false;";
    }

    internal static class Logging {
        internal const string Directory = "logs";
        internal const string FileNamePattern = "app-.log";

        /// <summary>
        /// Absolute path of the logs directory. Rooted at <see cref="AppContext.BaseDirectory"/>
        /// so the Serilog sink and the "open current log" feature always agree on the same
        /// location, regardless of the process's working directory (plan D4).
        /// </summary>
        internal static string DirectoryPath => SysPath.Combine(AppContext.BaseDirectory, Directory);
    }

    internal static class Application {
        internal const string Name = "InfoPhoenix";
    }

    internal static class Labels {
        internal static class NavigationMenuItems {
            // The nav item title comes from a `const` in an attribute, so it
            // cannot be localised at runtime (plan 2.6 known limitation).
            internal const string Configuration = "Configurações";
        }
    }
}
