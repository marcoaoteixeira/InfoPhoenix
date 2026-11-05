namespace Nameless.InfoPhoenix.Client.Tests.Views.Windows;

/// <summary>
/// WPF-UI's <c>ApplicationThemeManager</c>/<c>TitleBar</c> keep process-wide static theme
/// subscriptions. Any test that calls the real <c>ApplicationThemeManager.Apply</c>
/// (directly, or indirectly by constructing a real <c>MainWindow</c>, whose XAML includes a
/// <c>ui:TitleBar</c> that auto-subscribes) must not run concurrently with another such test
/// on a different thread, or one can raise a cross-thread access exception against the
/// other's window. xUnit runs test classes in the same collection sequentially, so every class
/// that touches this static state is tagged with this collection instead of a new per-test
/// seam.
/// </summary>
[CollectionDefinition(Name)]
public class WpfUiThemeTestCollection {
    public const string Name = "WPF-UI theme (serialized)";
}
