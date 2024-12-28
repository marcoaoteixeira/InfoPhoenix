using System.Windows;
using Nameless.InfoPhoenix.Application.Windows;
using Nameless.InfoPhoenix.Client.Contracts.Views.Windows;
using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.Dialogs;
using Nameless.InfoPhoenix.Domains.Dtos;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

public partial class DisplayDocumentSearchResultWindow : IDisplayDocumentSearchResultWindow {
    public DisplayDocumentSearchResultWindowViewModel ViewModel { get; }

    public DisplayDocumentSearchResultWindow(DisplayDocumentSearchResultWindowViewModel viewModel) {
        ViewModel = Prevent.Argument.Null(viewModel);

        DataContext = ViewModel;

        InitializeComponent();
    }

    public void SetTitle(string title)
        => ViewModel.Title = title;

    public void SetOwner(object? owner)
        => Owner = owner as Window ?? System.Windows.Application.Current.MainWindow;

    DialogResult IWindow.ShowDialog(StartupLocation startupLocation)
        => default;

    public void Show(StartupLocation startupLocation) {
        WindowStartupLocation = startupLocation.ToWindowStartupLocation();

        Show();
    }

    public void Initialize(DocumentDto[] documents, string[] highlightTerms)
        => ViewModel.InitializeCommand.Execute(new SearchedDocuments {
            Documents = documents,
            HighlightTerms = highlightTerms
        });

    private void CloseHandler(object _, RoutedEventArgs __)
        => Close();
}
