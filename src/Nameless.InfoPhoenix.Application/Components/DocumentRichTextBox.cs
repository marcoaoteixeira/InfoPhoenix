using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace Nameless.InfoPhoenix.Application.Components;

public sealed class DocumentRichTextBox : RichTextBox {
    private static readonly SolidColorBrush HighlightBrush = new(Colors.Yellow);

    public static readonly DependencyProperty SourceProperty =
        DependencyProperty.Register(nameof(Source),
                                    typeof(string),
                                    typeof(DocumentRichTextBox),
                                    new PropertyMetadata(SourceChangeHandler));

    public static readonly DependencyProperty HighlightTermsProperty =
        DependencyProperty.Register(nameof(HighlightTerms),
                                    typeof(string[]),
                                    typeof(DocumentRichTextBox),
                                    new PropertyMetadata(HighlightChangeHandler));

    public string Source {
        get => GetValue(SourceProperty) as string ?? string.Empty;
        set => SetValue(SourceProperty, value);
    }

    public string[] HighlightTerms {
        get => GetValue(HighlightTermsProperty) as string[] ?? [];
        set => SetValue(HighlightTermsProperty, value);
    }

    public void ApplyHighlight(params string[] highlightTerms) {
        if (highlightTerms.Length == 0) { return; }

        var textRange = new TextRange(Document.ContentStart,
                                      Document.ContentEnd);
        textRange.ClearAllProperties();

        var content = textRange.Text;

        foreach (var highlightTerm in highlightTerms) {
            if (!Regex.IsMatch(content, highlightTerm, RegexOptions.IgnoreCase)) { continue; }

            var startPointer = Document.ContentStart;
            while (startPointer is not null) {
                if (startPointer.CompareTo(Document.ContentEnd) == 0) {
                    break;
                }

                var value = startPointer.GetTextInRun(LogicalDirection.Forward);
                var highlightTermPosition = value.IndexOf(highlightTerm, StringComparison.CurrentCultureIgnoreCase);

                // Didn't find the highlight text in this Run, skip to next one.
                if (highlightTermPosition < 0) {
                    startPointer = startPointer.GetNextContextPosition(LogicalDirection.Forward);
                    continue;
                }

                // Setting up the pointer here at this matched index
                startPointer = startPointer.GetPositionAtOffset(highlightTermPosition);

                if (startPointer is null) { break; }

                var nextPointer = startPointer.GetPositionAtOffset(highlightTerm.Length);
                var searchedTextRange = new TextRange(startPointer, nextPointer);

                // Finally, highlight it
                searchedTextRange.ApplyPropertyValue(TextElement.BackgroundProperty, HighlightBrush);

                // Move next
                startPointer = startPointer.GetNextContextPosition(LogicalDirection.Forward);
            }
        }
    }

    private static void SourceChangeHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args) {
        if (dependencyObject is not DocumentRichTextBox richTextBox) { return; }

        using var stream = richTextBox.Source.ToMemoryStream();

        var range = new TextRange(richTextBox.Document.ContentStart, richTextBox.Document.ContentEnd);
        range.Load(stream, DataFormats.Text);

        richTextBox.ApplyHighlight(richTextBox.HighlightTerms);
    }

    private static void HighlightChangeHandler(DependencyObject dependencyObject, DependencyPropertyChangedEventArgs args) {
        if (dependencyObject is not DocumentRichTextBox richTextBox) { return; }

        richTextBox.ApplyHighlight(richTextBox.HighlightTerms);
    }
}
