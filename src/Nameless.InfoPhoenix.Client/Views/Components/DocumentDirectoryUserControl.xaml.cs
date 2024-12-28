using System.IO;
using System.Windows;
using System.Windows.Input;
using Nameless.InfoPhoenix.Application;
using Nameless.InfoPhoenix.Domains.Dtos;

namespace Nameless.InfoPhoenix.Client.Views.Components;

public partial class DocumentDirectoryUserControl  {

    public static readonly DependencyProperty DocumentDirectoryProperty
        = DependencyProperty.Register(nameof(DocumentDirectory),
                                      propertyType: typeof(DocumentDirectoryDto),
                                      ownerType: typeof(DocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: DocumentDirectoryDto.Empty));

    public static readonly DependencyProperty DoubleClickCommandProperty
        = DependencyProperty.Register(name: nameof(DoubleClickCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public static readonly DependencyProperty DeleteCommandProperty
        = DependencyProperty.Register(name: nameof(DeleteCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public static readonly DependencyProperty EditCommandProperty
        = DependencyProperty.Register(name: nameof(EditCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public static readonly DependencyProperty IndexCommandProperty
        = DependencyProperty.Register(name: nameof(IndexCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public static readonly DependencyProperty OpenCommandProperty
        = DependencyProperty.Register(name: nameof(OpenCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public static readonly DependencyProperty UpdateCommandProperty
        = DependencyProperty.Register(name: nameof(UpdateCommand),
                                      propertyType: typeof(ICommand),
                                      ownerType: typeof(DocumentDirectoryUserControl),
                                      typeMetadata: new PropertyMetadata(defaultValue: EmptyCommand.Instance));

    public Visibility IsMissingBadgeVisible
        => Directory.Exists(DocumentDirectory.DirectoryPath)
            ? Visibility.Collapsed
            : Visibility.Visible;

    public bool IsDropDownButtonEnabled
        => !Directory.Exists(DocumentDirectory.DirectoryPath);

    public DocumentDirectoryDto DocumentDirectory {
        get => GetValue(DocumentDirectoryProperty) as DocumentDirectoryDto ?? DocumentDirectoryDto.Empty;
        set => SetValue(DocumentDirectoryProperty, value);
    }

    public ICommand DeleteCommand {
        get => (ICommand)GetValue(DeleteCommandProperty);
        set => SetValue(DeleteCommandProperty, value);
    }

    public ICommand DoubleClickCommand {
        get => (ICommand)GetValue(DoubleClickCommandProperty);
        set => SetValue(DoubleClickCommandProperty, value);
    }

    public ICommand EditCommand {
        get => (ICommand)GetValue(EditCommandProperty);
        set => SetValue(EditCommandProperty, value);
    }
    public ICommand IndexCommand {
        get => (ICommand)GetValue(IndexCommandProperty);
        set => SetValue(IndexCommandProperty, value);
    }

    public ICommand OpenCommand {
        get => (ICommand)GetValue(OpenCommandProperty);
        set => SetValue(OpenCommandProperty, value);
    }

    public ICommand UpdateCommand {
        get => (ICommand)GetValue(UpdateCommandProperty);
        set => SetValue(UpdateCommandProperty, value);
    }

    public DocumentDirectoryUserControl() {
        InitializeComponent();
    }

    private void DeleteHandler(object sender, RoutedEventArgs e)
        => DeleteCommand.Execute(DocumentDirectory);

    private void DoubleClickHandler(object sender, MouseButtonEventArgs e)
        => DoubleClickCommand.Execute(DocumentDirectory);

    private void EditHandler(object sender, RoutedEventArgs e)
        => EditCommand.Execute(DocumentDirectory);

    private void IndexHandler(object sender, RoutedEventArgs e)
        => IndexCommand.Execute(DocumentDirectory);

    private void OpenHandler(object sender, RoutedEventArgs e)
        => OpenCommand.Execute(DocumentDirectory);

    private void UpdateHandler(object sender, RoutedEventArgs e)
        => UpdateCommand.Execute(DocumentDirectory);
}