using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.Contracts;
using Nameless.InfoPhoenix.Client.Features.GetDocumentDirectory;
using Nameless.InfoPhoenix.Client.Resources.l10n;
using Nameless.Mediator;
using Nameless.ObjectModel;
using Nameless.Validation;
using Nameless.Windows.Dialogs.FileSystem;
using Nameless.Windows.Localization;
using Nameless.Windows.Mvvm;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

public partial class DocumentDirectoryWindowViewModel : ViewModel {
    private readonly IFileSystemDialog _fileSystemDialog;
    private readonly IMediator _mediator;
    private readonly ITranslator _translator;
    private readonly IValidator _validator;

    private Guid DocumentDirectoryID { get; set; }

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    [Display(Name = nameof(Label), ResourceType = typeof(Displays))]
    [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validations))]
    [MinLength(1)]
    [MaxLength(1024)]
    private string _label = string.Empty;

    [ObservableProperty]
    [Display(Name = nameof(DirectoryPath), ResourceType = typeof(Displays))]
    [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(Validations))]
    [MinLength(3)]
    [MaxLength(1024)]
    private string _directoryPath = string.Empty;

    [ObservableProperty]
    [Display(Name = nameof(DisplayPosition), ResourceType = typeof(Displays))]
    [Range(minimum: 0, maximum: short.MaxValue)]
    private int _displayPosition;

    public DocumentDirectoryWindowViewModel(IFileSystemDialog fileSystemDialog, IMediator mediator, ITranslator translator, IValidator validator) {
        _fileSystemDialog = Throws.When.Null(fileSystemDialog);
        _mediator = Throws.When.Null(mediator);
        _translator = Throws.When.Null(translator);
        _validator = Throws.When.Null(validator);
    }

    [RelayCommand]
    private async Task UpdateViewModelAsync(Guid documentDirectoryID, CancellationToken cancellationToken) {
        if (documentDirectoryID == Guid.Empty) {
            return;
        }

        var documentDirectory = await _mediator.ExecuteAsync(new GetDocumentDirectoryRequest { ID = documentDirectoryID }, cancellationToken)
                                               .SkipContextSync();

        DocumentDirectoryID = documentDirectory.ID;
        Label = documentDirectory.Label;
        DirectoryPath = documentDirectory.DirectoryPath;
        DisplayPosition = documentDirectory.DisplayPosition;
    }

    [RelayCommand]
    private Task PickDirectoryPathAsync() {
        var result = _fileSystemDialog.OpenDirectory(opts => {
            opts.Title = _translator["FileSystemDialog_Title"];
        });

        var directoryPath = result.SingleOrDefault();
        if (directoryPath is not null) {
            DirectoryPath = directoryPath;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveAsync(CancellationToken cancellationToken) {
        var validationResult = await _validator.ValidateAsync(this, cancellationToken);
        if (validationResult.Failure) {
            
        }
    }

    private Task CreateNewDocumentDirectoryAsync() {
        return Task.CompletedTask;
    }

    private Task UpdateDocumentDirectoryAsync() {
        return Task.CompletedTask;
    }
}
