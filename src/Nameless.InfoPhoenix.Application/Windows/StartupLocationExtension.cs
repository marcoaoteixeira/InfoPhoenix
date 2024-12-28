using System.Windows;

namespace Nameless.InfoPhoenix.Application.Windows;

public static class StartupLocationExtension {
    public static WindowStartupLocation ToWindowStartupLocation(this StartupLocation self)
        => self switch {
            StartupLocation.Manual => WindowStartupLocation.Manual,
            StartupLocation.CenterScreen => WindowStartupLocation.CenterScreen,
            StartupLocation.CenterOwner => WindowStartupLocation.CenterOwner,
            _ => WindowStartupLocation.Manual
        };
}