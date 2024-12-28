using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Nameless.InfoPhoenix.Client.Resources;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Get;
using Nameless.InfoPhoenix.Domains.UseCases.DocumentDirectories.Save;
using Ookii.Dialogs.Wpf;

namespace Nameless.InfoPhoenix.Client.ViewModels.Forms;

public partial class DocumentDirectoryFormViewModel : ViewModelBase {
    private readonly IMediator _mediator;

    private Guid _id = Guid.Empty;

    [ObservableProperty]
    private string _title = string.Empty;

    [ObservableProperty]
    [Display(Name = "DocumentDirectory_Label", ResourceType = typeof(DisplayResource))]
    [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ValidationResource))]
    [MinLength(8)]
    [MaxLength(1024)]
    private string _label = string.Empty;

    [ObservableProperty]
    [Display(Name = "DocumentDirectory_DirectoryPath", ResourceType = typeof(DisplayResource))]
    [Required(ErrorMessageResourceName = "Required", ErrorMessageResourceType = typeof(ValidationResource))]
    [MinLength(8)]
    [MaxLength(1024)]
    private string _directoryPath = string.Empty;

    [ObservableProperty]
    [Display(Name = "DocumentDirectory_Order", ResourceType = typeof(DisplayResource))]
    [Range(minimum: 0, maximum: 999)]
    private int _order;

    public DocumentDirectoryFormViewModel(IMediator mediator) {
        _mediator = Prevent.Argument.Null(mediator);
    }

    [RelayCommand]
    private async Task InitializeAsync(Guid id) {
        _id = id;

        var documentDirectory = await _mediator.Send(new GetDocumentDirectoryRequest { ID = id });

        Label = documentDirectory.Label;
        DirectoryPath = documentDirectory.DirectoryPath;
        Order = documentDirectory.Order;
    }

    [RelayCommand]
    private Task PickDirectoryPathAsync() {
        var dialog = new VistaFolderBrowserDialog {
            Multiselect = false
        };

        var result = dialog.ShowDialog(System.Windows.Application.Current.MainWindow);
        if (result.GetValueOrDefault()) {
            DirectoryPath = dialog.SelectedPath;
        }

        return Task.CompletedTask;
    }

    [RelayCommand]
    private async Task SaveAsync() {
        ValidateAllProperties();

        if (HasErrors) { return; }

        var request = new SaveDocumentDirectoryRequest {
            ID = _id,
            Label = Label,
            DirectoryPath = DirectoryPath,
            Order = Order,
        };

        await _mediator.Send(request, CancellationToken.None);
    }
}