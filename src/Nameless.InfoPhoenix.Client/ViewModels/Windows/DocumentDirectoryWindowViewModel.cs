using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Nameless.InfoPhoenix.Client.Resources.l10n;
using Nameless.Mediator;
using Nameless.Windows.Dialogs.FileSystem;
using Nameless.Windows.Mvvm;

namespace Nameless.InfoPhoenix.Client.ViewModels.Windows;

public partial class DocumentDirectoryWindowViewModel : ViewModel {
    private readonly IFileSystemDialog _fileSystemDialog;
    private readonly IMediator _mediator;

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

    public DocumentDirectoryWindowViewModel(IFileSystemDialog fileSystemDialog, IMediator mediator) {
        _fileSystemDialog = fileSystemDialog;
        _mediator = mediator;
    }

    [RelayCommand]
    private Task UpdateViewModelAsync(Guid documentDirectoryID) {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task PickDirectoryPathAsync() {
        return Task.CompletedTask;
    }

    [RelayCommand]
    private Task SaveAsync() {
        return Task.CompletedTask;
    }

    private Task CreateNewDocumentDirectoryAsync() {
        return Task.CompletedTask;
    }

    private Task UpdateDocumentDirectoryAsync() {
        return Task.CompletedTask;
    }
}
