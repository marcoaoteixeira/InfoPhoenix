using Nameless.Windows.UI;

namespace Nameless.InfoPhoenix.Client.Infrastructure.UI.Contracts;

public interface IDocumentDirectoryWindow : IWindow {
    void SetTitle(string title);
    void SetDocumentDirectory(Guid id);
}