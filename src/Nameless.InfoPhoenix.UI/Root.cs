using System.Windows.Xps.Packaging;

namespace Nameless.InfoPhoenix.UI {
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

        public static class Defaults {
            #region Public Static Read-Only Properties

            public static TimeSpan SnackbarTimeout { get; } = TimeSpan.FromSeconds(5);

            #endregion
        }

        public static class Emoji {
            #region Public Constants

            public const string INFORMATION = "\u00af\\_(ツ)_/\u00af";
            public const string WARNING = "(\u25cf\u00b4\u2313`\u25cf)";
            public const string ERROR = "(ᗒᗣᗕ)՞";
            public const string CAUTION = "(\u0361\u00b0 \u035cʖ \u0361\u00b0)";
            public const string SUCCESS = "( ๑‾\u0300\u25e1‾\u0301)σ\"";

            #endregion
        }

        #endregion
    }
}
