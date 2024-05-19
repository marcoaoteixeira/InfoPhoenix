using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.UI.Controls;

namespace Nameless.InfoPhoenix.Client.Views.Windows {
    public partial class ShowSearchResultGroupWindow {
        #region Public Read-Only Properties

        public ShowSearchResultGroupWindowViewModel ViewModel { get; }

        #endregion

        #region Public Constructors

        public ShowSearchResultGroupWindow(ShowSearchResultGroupWindowViewModel viewModel) {

            ViewModel = Guard.Against.Null(viewModel, nameof(viewModel));
            Title = $"Resultado da Pesquisa | {ViewModel.Label}";
            DataContext = ViewModel;

            InitializeComponent();
        }

        private void DocumentContentRichTextBoxLoadHandler(object sender, RoutedEventArgs e) {
            HighlightText(sender);
        }

        private void DocumentContentRichTextBoxChangeHandler(object sender, RoutedEventArgs e) {
            //HighlightText(sender);
        }

        private static void HighlightText(object sender) {
            if (sender is RichTextBox richTextBox) {
                var textRange = new TextRange(richTextBox.Document.ContentStart,
                                              richTextBox.Document.ContentEnd);
                textRange.ClearAllProperties();

                var highlight = "INCONSTITUCIONALIDADE";
                var content = textRange.Text;
                var regex = Regex.Match(content, highlight);
                var count = regex.Captures.Count;

                for (var startPointer = richTextBox.Document.ContentStart;
                     startPointer.CompareTo(richTextBox.Document.ContentEnd) <= 0;
                     startPointer = startPointer.GetNextContextPosition(LogicalDirection.Forward)) {
                    if (startPointer.CompareTo(richTextBox.Document.ContentEnd) == 0) {
                        break;
                    }

                    var value = startPointer.GetTextInRun(LogicalDirection.Forward);
                    var indexOfValue = value.IndexOf(highlight, StringComparison.CurrentCultureIgnoreCase);

                    if (indexOfValue < 0) { continue; }

                    //setting up the pointer here at this matched index
                    startPointer = startPointer.GetPositionAtOffset(indexOfValue);
                    
                    if (startPointer is null) { break; }

                    var nextPointer = startPointer.GetPositionAtOffset(highlight.Length);
                    var searchedTextRange = new TextRange(startPointer, nextPointer);
                    //color up 
                    searchedTextRange.ApplyPropertyValue(TextElement.BackgroundProperty, new SolidColorBrush(Colors.Yellow));
                }
            }
        }

        #endregion

        #region Private Methods

        private void CloseHandler(object _, RoutedEventArgs __)
            => Close();

        #endregion
    }
}
