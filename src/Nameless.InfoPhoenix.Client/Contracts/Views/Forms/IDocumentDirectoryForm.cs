using Nameless.InfoPhoenix.Application.Windows;

namespace Nameless.InfoPhoenix.Client.Contracts.Views.Forms;

public interface IDocumentDirectoryForm : IWindow {
    void Initialize(Guid documentDirectoryID);
}