using System.Windows.Media;
using Nameless.InfoPhoenix.Dialogs;
using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Application;
public static class DialogIconExtension {
    public static SymbolRegular ToSymbolRegular(this DialogIcon self)
        => self switch {
            DialogIcon.Error => SymbolRegular.ChatDismiss24,
            DialogIcon.Attention => SymbolRegular.HandLeftChat28,
            DialogIcon.Information => SymbolRegular.ChatSparkle48,
            DialogIcon.Question => SymbolRegular.ChatHelp24,
            DialogIcon.Warning => SymbolRegular.ChatWarning24,
            _ => SymbolRegular.Chat32
        };

    public static Brush ToBrush(this DialogIcon self)
        => self switch {
            DialogIcon.Error => Brushes.Red,
            DialogIcon.Attention => Brushes.Gold,
            DialogIcon.Information => Brushes.CornflowerBlue,
            DialogIcon.Question => Brushes.CornflowerBlue,
            DialogIcon.Warning => Brushes.Orange,
            _ => Brushes.WhiteSmoke
        };
}
