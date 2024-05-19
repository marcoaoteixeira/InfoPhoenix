using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Domain.Dtos;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows {
    public partial class ShowSearchResultGroupWindowViewModel : ObservableObject {
        #region Private Read-Only Fields

        private readonly SearchResultEntryGroupDto _searchResultEntryGroup;
        private readonly int _lastIndex;

        #endregion

        #region Private Fields

        private int _currentIndex;

        #endregion

        #region Private Fields (Observables)

        [ObservableProperty]
        private bool _hasFirst;

        [ObservableProperty]
        private bool _hasPrevious;

        [ObservableProperty]
        private bool _hasNext;

        [ObservableProperty]
        private bool _hasLast;

        [ObservableProperty]
        private string _currentIcon = string.Empty;

        [ObservableProperty]
        private SearchResultEntryDto _current;

        [ObservableProperty]
        private string _content = string.Empty;

        #endregion

        #region Public Properties

        public string Label { get; }

        #endregion

        #region Public Constructors

        public ShowSearchResultGroupWindowViewModel(SearchResultEntryGroupDto searchResultEntryGroup) {
            _searchResultEntryGroup = Guard.Against.Null(searchResultEntryGroup, nameof(searchResultEntryGroup));
            
            _currentIndex = 0;
            _lastIndex = searchResultEntryGroup.Count - 1;

            Label = searchResultEntryGroup.DocumentDirectoryLabel;
            Current = searchResultEntryGroup.ElementAtOrDefault(_currentIndex) ?? SearchResultEntryDto.Empty;
            CurrentIcon = GetIcon(Current.DocumentFilePath);
            Content = Current.DocumentContent;

            UpdateMovement();
        }

        #endregion

        #region Private Static Methods

        private static string GetIcon(string filePath)
            => Path.GetExtension(filePath) switch {
                ".doc" => "pack://application:,,,/Resources/doc_file_icon.png",
                ".docx" => "pack://application:,,,/Resources/docx_file_icon.png",
                ".rtf" => "pack://application:,,,/Resources/rtf_file_icon.png",
                _ => "pack://application:,,,/Resources/doc_file_icon.png",
            };
        
        #endregion

        #region Private Methods

        private void UpdateMovement() {
            HasFirst = _currentIndex > 0;
            HasPrevious = _currentIndex > 0;
            HasNext = _currentIndex < _lastIndex;
            HasLast = _currentIndex < _lastIndex;
        }

        #endregion

        #region Private Methods (Commands)

        [RelayCommand]
        private Task DisplaySearchResultEntryAsync(ArrayMovement movement) {
            _currentIndex = movement switch {
                ArrayMovement.First => 0,
                ArrayMovement.Previous => _currentIndex - 1 >= 0 ? _currentIndex - 1 : 0,
                ArrayMovement.Next => _currentIndex + 1 <= _lastIndex ? _currentIndex + 1 : _lastIndex,
                ArrayMovement.Last => _lastIndex,
                _ => 0
            };

            UpdateMovement();

            Current = _searchResultEntryGroup[_currentIndex];
            CurrentIcon = GetIcon(Current.DocumentFilePath);
            Content = Current.DocumentContent;

            return Task.CompletedTask;
        }

        #endregion
    }
}
