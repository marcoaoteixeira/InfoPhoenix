using Nameless.InfoPhoenix.Application.Windows;

namespace Nameless.InfoPhoenix.Client.Contracts.Views.Windows;

public interface IDocumentViewerWindow : IWindow {
    void SetDocumentFilePath(string filePath);
}