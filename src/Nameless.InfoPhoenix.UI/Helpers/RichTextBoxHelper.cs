using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace Nameless.InfoPhoenix.UI.Helpers {
    public class RichTextBoxHelper : DependencyObject {
        #region Public Static Methods

        public static string GetDocumentContent(DependencyObject dependencyObject)
            => (string)dependencyObject.GetValue(DocumentContentProperty);

        public static void SetDocumentContent(DependencyObject dependencyObject, string value)
            => dependencyObject.SetValue(DocumentContentProperty, value);

        #endregion

        #region Public Static Properties

        public static readonly DependencyProperty DocumentContentProperty =
            DependencyProperty.RegisterAttached(name: "DocumentContent",
                                                propertyType: typeof(string),
                                                ownerType: typeof(RichTextBoxHelper),
                                                defaultMetadata: new FrameworkPropertyMetadata {
                                                    BindsTwoWayByDefault = true,
                                                    PropertyChangedCallback = DocumentContentChangeHandler
                                                });

        #endregion

        #region Private Static Methods

        private static void DocumentContentChangeHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs e) {
            if (dependencyObject is not RichTextBox richTextBox) { return; }

            // Parse the XAML to a document (or use XamlReader.Parse())
            var documentXaml = GetDocumentContent(richTextBox);
            var documentXamlBuffer = Encoding.UTF8.GetBytes(documentXaml);

            var flowDocument = new FlowDocument();
            var textRange = new TextRange(flowDocument.ContentStart,
                                          flowDocument.ContentEnd);

            textRange.Load(new MemoryStream(documentXamlBuffer), DataFormats.Text);

            // Set the document
            richTextBox.Document = flowDocument;
        }

        #endregion
    }
}
