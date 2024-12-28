using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Application;

internal static class Constants {
    internal static class Icons {
        internal static IconElement Caution = new SymbolIcon(SymbolRegular.Warning28);
        internal static IconElement Danger = new SymbolIcon(SymbolRegular.ShieldError24);
        internal static IconElement Info = new SymbolIcon(SymbolRegular.LightbulbFilament24);
        internal static IconElement Regular = new SymbolIcon(SymbolRegular.Book24);
        internal static IconElement Success = new SymbolIcon(SymbolRegular.ThumbLike28);
    }

    internal static class Presentation {
        internal static readonly TimeSpan SnackbarTimeout = TimeSpan.FromSeconds(5);
    }
}