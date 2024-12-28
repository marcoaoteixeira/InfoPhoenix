using System.Windows;
using System.Windows.Input;
using Nameless.InfoPhoenix.Application;
using Nameless.InfoPhoenix.Domains.Dtos;

namespace Nameless.InfoPhoenix.Client.Views.Components;

public partial class DocumentDirectorySearchResultUserControl {
    public static readonly DependencyProperty DocumentDirectoryProperty =
        DependencyProperty.Register(name: nameof(DocumentDirectory),
                                    propertyType: typeof(DocumentDirectoryDto),
                                    ownerType: typeof(DocumentDirectorySearchResultUserControl),
                                    typeMetadata: new PropertyMetadata(defaultValue: DocumentDirectoryDto.Empty));

    public static readonly DependencyProperty ShowDocumentsCommandProperty =
        DependencyProperty.Register(name: nameof(ShowDocumentsCommand),
                                    propertyType: typeof(ICommand),
                                    ownerType: typeof(DocumentDirectorySearchResultUserControl),
                                    typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public DocumentDirectoryDto DocumentDirectory {
        get => GetValue(DocumentDirectoryProperty) as DocumentDirectoryDto ?? DocumentDirectoryDto.Empty;
        set => SetValue(DocumentDirectoryProperty, value);
    }

    public ICommand ShowDocumentsCommand {
        get => (ICommand)GetValue(ShowDocumentsCommandProperty);
        set => SetValue(ShowDocumentsCommandProperty, value);
    }

    public DocumentDirectorySearchResultUserControl() {
        InitializeComponent();
    }

    private void ShowDocumentsHandler(object sender, RoutedEventArgs args)
        => ShowDocumentsCommand.Execute(DocumentDirectory);
}
