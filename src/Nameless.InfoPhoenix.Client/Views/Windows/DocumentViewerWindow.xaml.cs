using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Nameless.InfoPhoenix.Client.ViewModels.Windows;
using Nameless.InfoPhoenix.DTOs;
using Nameless.WPF.DependencyInjection;
using Nameless.WPF.Mvvm;
using Nameless.WPF.Windows;

namespace Nameless.InfoPhoenix.Client.Views.Windows;

[ServiceLifetime(Lifetime = ServiceLifetime.Transient)]
public partial class DocumentViewerWindow : IDocumentViewerWindow, IHasViewModel<DocumentViewerWindowViewModel> {
    public DocumentViewerWindowViewModel ViewModel { get; }

    public DocumentViewerWindow(DocumentViewerWindowViewModel viewModel) {
        ViewModel = viewModel;

        DataContext = ViewModel;

        InitializeComponent();
    }

    public void SetTitle(string title) {
        ViewModel.Title = title;
    }

    public void Initialize(DocumentDto document) {
        ViewModel.InitializeCommand.Execute(document);
    }

    public void Show(WindowStartupLocation startupLocation) {
        if (Application.Current.MainWindow is not null) {
            Owner = Application.Current.MainWindow;
        }

        WindowStartupLocation = startupLocation;

        Show();
    }
}

public interface IDocumentViewerWindow : IWindow {
    void SetTitle(string title);

    void Initialize(DocumentDto document);
}