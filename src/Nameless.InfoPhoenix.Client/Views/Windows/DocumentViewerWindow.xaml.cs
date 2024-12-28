using System.Windows;
using Nameless.InfoPhoenix.Application.Windows;
using Nameless.InfoPhoenix.Client.Contracts.Views.Windows;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.Dialogs;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class DocumentViewerWindow : IDocumentViewerWindow {
    private string _documentFilePath = string.Empty;

    public DocumentViewerWindowViewModel ViewModel { get; set; }

    public DocumentViewerWindow(DocumentViewerWindowViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }

    public void SetTitle(string title)
        => ViewModel.Title = title;

    public void SetOwner(object? owner)
        => Owner = owner as Window ?? System.Windows.Application.Current.MainWindow;

    public DialogResult ShowDialog(StartupLocation startupLocation)
        => Dialogs.DialogResult.Yes;

    public void Show(StartupLocation startupLocation) {
        WindowStartupLocation = startupLocation.ToWindowStartupLocation();

        Show();

        if (!string.IsNullOrWhiteSpace(_documentFilePath)) {
            ViewModel.InitializeCommand.Execute(_documentFilePath);
        }
    }

    public void SetDocumentFilePath(string filePath)
        => _documentFilePath = filePath;
}