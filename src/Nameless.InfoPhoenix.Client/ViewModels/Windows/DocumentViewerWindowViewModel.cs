using System.IO;
using System.IO.Packaging;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Nameless.InfoPhoenix.Domains.UseCases.Documents.Convert;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

public partial class DocumentViewerWindowViewModel : ViewModelBase {
    private readonly IMediator _mediator;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private FixedDocumentSequence? _document;

    public DocumentViewerWindowViewModel(IMediator mediator) {
        _mediator = Prevent.Argument.Null(mediator);
    }

    [RelayCommand]
    private async Task InitializeAsync(string filePath, CancellationToken cancellationToken) {
        var result = await _mediator.Send(new ConvertDocumentToXpsFileRequest {
            FilePath = filePath,
        }, cancellationToken);

        if (result is { HasErrors: false, Value.Length: > 0 }) {
            var doc = new XpsDocument(result.Value,
                                      FileAccess.Read,
                                      CompressionOption.NotCompressed);

            Document = doc.GetFixedDocumentSequence();
        }
    }
}