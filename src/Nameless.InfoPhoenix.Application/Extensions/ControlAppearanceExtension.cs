using Wpf.Ui.Controls;

namespace Nameless.InfoPhoenix.Application;

public static class ControlAppearanceExtension {
    public static string GetTitle(this ControlAppearance self)
        => self switch {
            ControlAppearance.Caution => "Atenção",
            ControlAppearance.Danger => "Erro",
            ControlAppearance.Info => "Informação",
            ControlAppearance.Success => "Sucesso",
            _ => "Notificação"
        };

    public static IconElement GetIcon(this ControlAppearance self)
        => self switch {
            ControlAppearance.Caution => Constants.Icons.Caution,
            ControlAppearance.Danger => Constants.Icons.Danger,
            ControlAppearance.Info => Constants.Icons.Info,
            ControlAppearance.Success => Constants.Icons.Success,
            _ => Constants.Icons.Regular
        };
}