namespace Nameless.InfoPhoenix {
    /// <summary>
    /// This class was defined to be an entrypoint for this project assembly.
    /// 
    /// *** DO NOT IMPLEMENT ANYTHING HERE ***
    /// 
    /// But, it's allow to use this class as a repository for all constants or
    /// default values that we'll use throughout this project.
    /// </summary>
    public static class Root {
        #region Public Static Inner Classes

        public static class Names {
            #region Public Constants

            public const string APPLICATION = "Info Phoenix";
            public const string DATABASE = "database.db";
            public const string LOG_FILE = "info_phoenix.log";
            public const string BACKUP_FOLDER = "backup";

            #endregion
        }

        public static class Defaults {
            #region Public Constants

            
            public const string DIRECTORY_SEPARATOR_CHAR = "\\";
            public const string APPLICATION_THEME = "Light";

            #endregion
        }

        #endregion
    }
}
