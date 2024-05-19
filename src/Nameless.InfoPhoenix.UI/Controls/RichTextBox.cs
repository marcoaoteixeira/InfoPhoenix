using System.Windows;
using System.Windows.Documents;

namespace Nameless.InfoPhoenix.UI.Controls {
    public sealed class RichTextBox : System.Windows.Controls.RichTextBox {
        #region Public Static Read-Only Fields (Dependency)

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register(nameof(Source),
                                        typeof(string),
                                        typeof(RichTextBox),
                                        new PropertyMetadata(SourceChangeHandler));

        #endregion

        #region Public Properties

        public string Source {
            get => GetValue(SourceProperty) as string ?? string.Empty;
            set => SetValue(SourceProperty, value);
        }

        #endregion

        #region Private Static Methods

        private static void SourceChangeHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args) {
            if (dependencyObject is not RichTextBox richTextBox) { return; }
            
            using var stream = richTextBox.Source.ToMemoryStream();

            var range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
            range.Load(stream, DataFormats.Text);
        }

        #endregion
    }
}
