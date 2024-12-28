using Nameless.InfoPhoenix.Application.Windows;
using Nameless.InfoPhoenix.Domains.Dtos;

namespace Nameless.InfoPhoenix.Client.Contracts.Views.Windows;

public interface IDisplayDocumentSearchResultWindow : IWindow {
    void Initialize(DocumentDto[] documents, string[] highlightTerms);
}
