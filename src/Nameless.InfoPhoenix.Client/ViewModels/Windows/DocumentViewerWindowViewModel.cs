using System.Windows.Documents;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using Nameless.InfoPhoenix.Client.Data.DTOs;
using Nameless.Mediator;
using Nameless.Windows.Mvvm;

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
    private Task InitializeAsync(DocumentDto document, CancellationToken cancellationToken) {
        return Task.CompletedTask;
    }
}
