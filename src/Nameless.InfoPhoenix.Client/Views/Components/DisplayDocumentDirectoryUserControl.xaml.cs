using System.IO;
using System.Windows;
using System.Windows.Input;
using Nameless.InfoPhoenix.DTOs;
using Nameless.WPF.Infrastructure;

namespace Nameless.InfoPhoenix.Client.Views.Components;

public partial class DisplayDocumentDirectoryUserControl {
    public static readonly DependencyProperty DocumentDirectoryProperty
        = DependencyProperty.Register(nameof(DocumentDirectory),
                                      propertyType: typeof(DocumentDirectoryDto),
                                      ownerType: typeof(DisplayDocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: DocumentDirectoryDto.Empty));

    public static readonly DependencyProperty DoubleClickCommandProperty
        = DependencyProperty.Register(name: nameof(DoubleClickCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DisplayDocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public static readonly DependencyProperty EditCommandProperty
        = DependencyProperty.Register(name: nameof(EditCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DisplayDocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public static readonly DependencyProperty FetchDocumentsCommandProperty
        = DependencyProperty.Register(name: nameof(FetchDocumentsCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DisplayDocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public static readonly DependencyProperty IndexDocumentsCommandProperty
        = DependencyProperty.Register(name: nameof(IndexDocumentsCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DisplayDocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public static readonly DependencyProperty OpenInExplorerCommandProperty
        = DependencyProperty.Register(name: nameof(OpenInExplorerCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DisplayDocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public static readonly DependencyProperty DeleteCommandProperty
        = DependencyProperty.Register(name: nameof(DeleteCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DisplayDocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public DocumentDirectoryDto DocumentDirectory {
        get => GetValue(DocumentDirectoryProperty) as DocumentDirectoryDto ?? DocumentDirectoryDto.Empty;
        set => SetValue(DocumentDirectoryProperty, value);
    }

    public Visibility IsMissingBadgeVisible
        => Directory.Exists(DocumentDirectory.DirectoryPath)
            ? Visibility.Collapsed
            : Visibility.Visible;

    public bool IsDropDownButtonEnabled
        => !Directory.Exists(DocumentDirectory.DirectoryPath);

    public ICommand DoubleClickCommand {
        get => (ICommand)GetValue(DoubleClickCommandProperty);
        set => SetValue(DoubleClickCommandProperty, value);
    }

    public ICommand EditCommand {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }

    public ICommand FetchDocumentsCommand {
        get => (ICommand)GetValue(FetchDocumentsCommandProperty);
        set => SetValue(FetchDocumentsCommandProperty, value);
    }

    public ICommand IndexDocumentsCommand {
        get => (ICommand)GetValue(IndexDocumentsCommandProperty);
        set => SetValue(IndexDocumentsCommandProperty, value);
    }

    public ICommand OpenInExplorerCommand {
        get => (ICommand)GetValue(OpenInExplorerCommandProperty);
        set => SetValue(OpenInExplorerCommandProperty, value);
    }

    public ICommand DeleteCommand {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public DisplayDocumentDirectoryUserControl() {
        InitializeComponent();
    }

    private void DeleteHandler(object sender, RoutedEventArgs e) {
        DeleteCommand.Execute(DocumentDirectory);
    }

    private void DoubleClickHandler(object sender, MouseButtonEventArgs e) {
        DoubleClickCommand.Execute(DocumentDirectory);
    }

    private void EditHandler(object sender, RoutedEventArgs e) {
        EditCommand.Execute(DocumentDirectory);
    }

    private void IndexDocumentsHandler(object sender, RoutedEventArgs e) {
        IndexDocumentsCommand.Execute(DocumentDirectory);
    }

    private void OpenInExplorerHandler(object sender, RoutedEventArgs e) {
        OpenInExplorerCommand.Execute(DocumentDirectory);
    }

    private void FetchDocumentsHandler(object sender, RoutedEventArgs e) {
        FetchDocumentsCommand.Execute(DocumentDirectory);
    }
}
