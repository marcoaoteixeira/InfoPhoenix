using System.Windows;
using System.Windows.Input;
using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Infrastructure;

namespace Nameless.InfoPhoenix.Client.Components {
    public partial class SearchResultEntryGroupUserControl {
        #region Public Dependency Fields

        public static readonly DependencyProperty SearchResultEntryGroupProperty =
            DependencyProperty.Register(
                nameof(SearchResultEntryGroup),
                propertyType: typeof(SearchResultCollectionDto),
                ownerType: typeof(SearchResultEntryGroupUserControl),
                typeMetadata: new PropertyMetadata(defaultValue: SearchResultCollectionDto.Empty)
            );

        public static readonly DependencyProperty VisualizeCommandProperty =
            DependencyProperty.Register(
                name: nameof(VisualizeCommand),
                propertyType: typeof(ICommand),
                ownerType: typeof(SearchResultEntryGroupUserControl),
                typeMetadata: new PropertyMetadata(defaultValue: NullCommand.Instance)
            );

        #endregion

        #region Public Properties

        public SearchResultCollectionDto SearchResultEntryGroup {
            get => GetValue(SearchResultEntryGroupProperty) as SearchResultCollectionDto ?? SearchResultCollectionDto.Empty;
            set => SetValue(SearchResultEntryGroupProperty, value);
        }

        public ICommand VisualizeCommand {
            get => (ICommand)GetValue(VisualizeCommandProperty);
            set => SetValue(VisualizeCommandProperty, value);
        }

        #endregion

        #region Public Constructors

        public SearchResultEntryGroupUserControl() {
            InitializeComponent();
        }

        #endregion

        #region Private Methods

        private void VisualizeHandler(object _, RoutedEventArgs __)
            => VisualizeCommand.Execute(SearchResultEntryGroup);

        #endregion
    }
}
