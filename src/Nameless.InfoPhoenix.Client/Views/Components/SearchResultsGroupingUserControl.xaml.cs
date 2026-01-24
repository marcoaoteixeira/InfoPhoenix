using System.Windows;
using System.Windows.Input;
using Nameless.InfoPhoenix.DTOs;
using Nameless.WPF.Infrastructure;

namespace Nameless.InfoPhoenix.Client.Views.Components;

public partial class SearchResultsGroupingUserControl {
    public static readonly DependencyProperty EntryProperty =
        DependencyProperty.Register(
            name: nameof(Entry),
            propertyType: typeof(DocumentDirectoryDto),
            ownerType: typeof(SearchResultsGroupingUserControl),
            typeMetadata: new PropertyMetadata(defaultValue: DocumentDirectoryDto.Empty));

    public static readonly DependencyProperty ShowDocumentsCommandProperty =
        DependencyProperty.Register(
            name: nameof(ShowDocumentsCommand),
            propertyType: typeof(ICommand),
            ownerType: typeof(SearchResultsGroupingUserControl),
            typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public DocumentDirectoryDto Entry {
        get => GetValue(EntryProperty) as DocumentDirectoryDto ?? DocumentDirectoryDto.Empty;
        set => SetValue(EntryProperty, value);
    }

    public ICommand ShowDocumentsCommand {
        get => (ICommand)GetValue(ShowDocumentsCommandProperty);
        set => SetValue(ShowDocumentsCommandProperty, value);
    }

    public SearchResultsGroupingUserControl() {
        InitializeComponent();
    }

    private void ShowDocumentsHandler(object sender, RoutedEventArgs args) {
        ShowDocumentsCommand.Execute(Entry);
    }
}
