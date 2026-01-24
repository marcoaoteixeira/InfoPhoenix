using System.IO;
using System.IO.Packaging;
using System.Windows.Documents;
using System.Windows.Xps.Packaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.DTOs;
using Nameless.InfoPhoenix.UseCases.Documents.ConvertXps;
using Nameless.Mediator;
using Nameless.WPF.Mvvm;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

public partial class DocumentViewerWindowViewModel : ViewModel {
    private readonly IMediator _mediator;
    private readonly ILogger<DocumentViewerWindowViewModel> _logger;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    private FixedDocumentSequence? _document;

    public DocumentViewerWindowViewModel(IMediator mediator, ILogger<DocumentViewerWindowViewModel> logger) {
        _mediator = mediator;
        _logger = logger;
    }

    [RelayCommand]
    private async Task InitializeAsync(DocumentDto document, CancellationToken cancellationToken) {
        var request = new ConvertIntoXpsFormatRequest { FilePath = document.FilePath };
        var response = await _mediator.ExecuteAsync(request, cancellationToken);

        response.Match(
            onSuccess: value => {
                var doc = new XpsDocument(value.FilePath, FileAccess.Read, CompressionOption.NotCompressed);

                Document = doc.GetFixedDocumentSequence();
            },
            onFailure: errors => {

            }
        );
    }
}
