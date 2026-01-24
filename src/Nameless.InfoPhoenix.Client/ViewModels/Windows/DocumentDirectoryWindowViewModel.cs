using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.InfoPhoenix.Client.Resources;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.Create;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.Get;
using Nameless.InfoPhoenix.UseCases.DocumentDirectories.Update;
using Nameless.Mediator;
using Nameless.WPF.Dialogs.FileSystem;
using Nameless.WPF.Mvvm;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

public partial class DocumentDirectoryWindowViewModel : ViewModel {
    private readonly IFileSystemDialog _fileSystemDialog;
    private readonly IMediator _mediator;

    private Guid DocumentDirectoryID { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    [Display(Name = "DocumentDirectoryWindow_DocumentDirectory_Label", ResourceType = typeof(Strings))]
    [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validations))]
    [MinLength(1)]
    [MaxLength(1024)]
    private string _label = string.Empty;

    [ObservableProperty]
    [Display(Name = "DocumentDirectoryWindow_DocumentDirectory_DirectoryPath", ResourceType = typeof(Strings))]
    [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validations))]
    [MinLength(3)]
    [MaxLength(1024)]
    private string _directoryPath = string.Empty;

    [ObservableProperty]
    [Display(Name = "DocumentDirectoryWindow_DocumentDirectory_DisplayPosition", ResourceType = typeof(Strings))]
    [Range(minimum: 0, maximum: short.MaxValue)]
    private int _displayPosition;

    public DocumentDirectoryWindowViewModel(IFileSystemDialog fileSystemDialog, IMediator mediator) {
        _fileSystemDialog = fileSystemDialog;
        _mediator = mediator;
    }

    [RelayCommand]
    private async Task UpdateViewModelAsync(Guid documentDirectoryID) {
        if (documentDirectoryID == Guid.Empty) {
            Title = Strings.DocumentDirectoryWindowViewModel_CreateNewDocumentDirectory_Title;

            return;
        }

        Title = Strings.DocumentDirectoryWindowViewModel_EditDocumentDirectory_Title;

        var request = new GetDocumentDirectoryRequest { DocumentDirectoryID = documentDirectoryID };
        var response = await _mediator.ExecuteAsync(request, CancellationToken.None);

        if (!response.Success) { return; }

        DocumentDirectoryID = documentDirectoryID;
        Label = response.Value.Label;
        DirectoryPath = response.Value.DirectoryPath;
        DisplayPosition = response.Value.DisplayPosition;
    }

    [RelayCommand]
    private Task PickDirectoryPathAsync() {
        var result = _fileSystemDialog.OpenDirectory(opts => {
            opts.Title = Strings.DocumentDirectoryWindowViewModel_PickDirectoryDialog_Title;
        });

        var directoryPath = result.SingleOrDefault();
        if (directoryPath is not null) {
            DirectoryPath = directoryPath;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task SaveAsync() {
        ValidateAllProperties();

        if (HasErrors) { return Task.CompletedTask; }

        return DocumentDirectoryID == Guid.Empty
            ? CreateNewDocumentDirectoryAsync()
            : UpdateDocumentDirectoryAsync();
    }

    private async Task CreateNewDocumentDirectoryAsync() {
        var request = new CreateNewDocumentDirectoryRequest {
            Label = Label,
            DirectoryPath = DirectoryPath,
            DisplayPosition = DisplayPosition
        };

        _ = await _mediator.ExecuteAsync(request, CancellationToken.None);
    }

    private async Task UpdateDocumentDirectoryAsync() {
        var request = new UpdateDocumentDirectoryRequest {
            DocumentDirectoryID = DocumentDirectoryID,
            Label = Label,
            DirectoryPath = DirectoryPath,
            DisplayPosition = DisplayPosition
        };

        _ = await _mediator.ExecuteAsync(request, CancellationToken.None);
    }
}
