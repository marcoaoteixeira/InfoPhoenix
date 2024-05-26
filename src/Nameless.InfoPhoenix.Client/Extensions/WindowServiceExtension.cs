using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.Client.Views.Windows;
using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.UI;

namespace Nameless.InfoPhoenix.Client {
    public static class WindowServiceExtension {
        #region Public Static Methods

        public static void DisplaySearchResultWindow(this IWindowFactory self, SearchResultCollectionDto searchResultCollection, string[] highlightTerms) {
            if (!self.TryCreate<ShowSearchResultWindow>(owner: null,
                                                        dataContext: null,
                                                        out var window)) {
                throw new InvalidOperationException($"Error while creating {nameof(ShowSearchResultWindow)}");
            }

            if (window is IViewModelAware<ShowSearchResultWindowViewModel> viewModelAware) {
                viewModelAware.ViewModel.SetHighlightTerms(highlightTerms);
                viewModelAware.ViewModel.SetSearchResultCollection(searchResultCollection);
            }

            window.Loaded += (sender, _) => {
                if (sender is ShowSearchResultWindow showSearchResultWindow) {
                    showSearchResultWindow.ViewModel.Initialize();
                    showSearchResultWindow.CurrentDocumentRichTextBox.HighlightTerms = highlightTerms;
                }
            };

            window.Show();
        }

        public static void DisplayDocumentViewer(this IWindowFactory self, string filePath) {
            if (!self.TryCreate<DocumentViewerWindow>(owner: null,
                                                      dataContext: filePath,
                                                      out var window)) {
                return;
            }

            window.Loaded += (sender, _) => {
                if (sender is DocumentViewerWindow documentViewerWindow) {
                    documentViewerWindow.DisplayDocument(filePath);
                }
            };

            window.Show();
        }

        #endregion
    }
}
