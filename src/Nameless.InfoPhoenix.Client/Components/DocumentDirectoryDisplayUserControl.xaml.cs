using System.Windows;
using System.Windows.Input;
using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Infrastructure;

namespace Nameless.InfoPhoenix.Client.Components {
    public partial class DocumentDirectoryDisplayUserControl {
        #region Public Dependency Fields

        public static readonly DependencyProperty DocumentDirectoryProperty =
            DependencyProperty.Register(
                nameof(DocumentDirectory),
                propertyType: typeof(DocumentDirectoryDto),
                ownerType: typeof(DocumentDirectoryDisplayUserControl),
                typeMetadata: new PropertyMetadata(defaultValue: DocumentDirectoryDto.Empty)
            );

        public static readonly DependencyProperty SelectCommandProperty =
            DependencyProperty.Register(
                name: nameof(SelectCommand),
                propertyType: typeof(ICommand),
                ownerType: typeof(DocumentDirectoryDisplayUserControl),
                typeMetadata: new PropertyMetadata(defaultValue: NullCommand.Instance)
            );

        public static readonly DependencyProperty EditCommandProperty =
            DependencyProperty.Register(
                name: nameof(EditCommand),
                propertyType: typeof(ICommand),
                ownerType: typeof(DocumentDirectoryDisplayUserControl),
                typeMetadata: new PropertyMetadata(defaultValue: NullCommand.Instance)
            );

        public static readonly DependencyProperty IndexCommandProperty =
            DependencyProperty.Register(
                name: nameof(IndexCommand),
                propertyType: typeof(ICommand),
                ownerType: typeof(DocumentDirectoryDisplayUserControl),
                typeMetadata: new PropertyMetadata(defaultValue: NullCommand.Instance)
            );

        public static readonly DependencyProperty OpenCommandProperty =
            DependencyProperty.Register(
                name: nameof(OpenCommand),
                propertyType: typeof(ICommand),
                ownerType: typeof(DocumentDirectoryDisplayUserControl),
                typeMetadata: new PropertyMetadata(defaultValue: NullCommand.Instance)
            );

        public static readonly DependencyProperty DeleteCommandProperty =
            DependencyProperty.Register(
                name: nameof(DeleteCommand),
                propertyType: typeof(ICommand),
                ownerType: typeof(DocumentDirectoryDisplayUserControl),
                typeMetadata: new PropertyMetadata(defaultValue: NullCommand.Instance)
            );

        #endregion

        #region Public Properties

        public DocumentDirectoryDto DocumentDirectory {
            get => GetValue(DocumentDirectoryProperty) as DocumentDirectoryDto ?? DocumentDirectoryDto.Empty;
            set => SetValue(DocumentDirectoryProperty, value);
        }

        public Visibility IsMissingBadgeVisible => DocumentDirectory.Missing ? Visibility.Visible : Visibility.Collapsed;

        public bool IsContextMenuItemEnabled => !DocumentDirectory.Missing;

        public ICommand SelectCommand {
            get => (ICommand)GetValue(SelectCommandProperty);
            set => SetValue(SelectCommandProperty, value);
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

        public ICommand DeleteCommand {
            get => (ICommand)GetValue(DeleteCommandProperty);
            set => SetValue(DeleteCommandProperty, value);
        }

        #endregion

        #region Public Constructors

        public DocumentDirectoryDisplayUserControl() {
            InitializeComponent();
        }

        #endregion

        #region Private Methods

        private void SelectHandler(object sender, MouseButtonEventArgs e)
            => SelectCommand.Execute(DocumentDirectory);

        private void EditHandler(object sender, RoutedEventArgs e)
            => EditCommand.Execute(DocumentDirectory);

        private void IndexHandler(object sender, RoutedEventArgs e)
            => IndexCommand.Execute(DocumentDirectory);

        private void OpenHandler(object sender, RoutedEventArgs e)
            => OpenCommand.Execute(DocumentDirectory);

        private void DeleteHandler(object sender, RoutedEventArgs e)
            => DeleteCommand.Execute(DocumentDirectory);

        #endregion

    }
}
