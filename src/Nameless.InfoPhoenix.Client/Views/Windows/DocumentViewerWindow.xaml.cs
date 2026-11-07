using System.Windows;
using Nameless.InfoPhoenix.Client.Data.DTOs;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.Windows.UI;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class DocumentViewerWindow : IDocumentViewerWindow {
    public DocumentViewerWindowViewModel ViewModel { get; }

    public DocumentViewerWindow(DocumentViewerWindowViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }

    public void SetTitle(string title) {
        ViewModel.Title = title;
    }

    public void Initialize(DocumentDto document) {
        ViewModel.InitializeCommand.Execute(document);
    }

    public void Show(WindowStartupLocation startupLocation) {
        if (System.Windows.Application.Current.MainWindow is not null) {
            Owner = System.Windows.Application.Current.MainWindow;
        }

        WindowStartupLocation = startupLocation;

        Show();
    }
}

public interface IDocumentViewerWindow : IWindow {
    void SetTitle(string title);

    void Initialize(DocumentDto document);
}