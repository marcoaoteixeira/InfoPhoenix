using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MediatR;
using Nameless.InfoPhoenix.Client.Objects;
using Nameless.InfoPhoenix.Configuration;
using Nameless.InfoPhoenix.Domain.Dtos;
using Nameless.InfoPhoenix.Domain.Requests;
using Nameless.InfoPhoenix.Domain.Responses;
using Nameless.InfoPhoenix.Infrastructure;
using Nameless.InfoPhoenix.Objects;
using Nameless.InfoPhoenix.UI;
using Nameless.InfoPhoenix.UI.Helpers;

namespace Nameless.InfoPhoenix.Client.ViewModels.Pages {
    public partial class SearchViewModel : ViewModelBase {
        #region Private Read-Only Fields

        private readonly IAppConfigurationContext _appConfigurationContext;
        private readonly IMediator _mediator;
        private readonly IWindowService _windowService;
        private readonly IPerformanceReporter _performanceReporter;

        #endregion

        #region Private Fields

        private bool _initialized;

        #endregion

        #region Private Fields (Observable)

        [ObservableProperty]
        private ObservableCollection<string> _searchHistory = [];

        [ObservableProperty]
        private List<SearchResultEntryGroupDto> _result = [];

        #endregion

        #region Public Constructors

        public SearchViewModel(
            IAppConfigurationContext appConfigurationContext,
            IMediator mediator,
            IPubSubService pubSubService,
            IPerformanceReporter performanceReporter,
            IWindowService windowService) : base(pubSubService) {
            _appConfigurationContext = Guard.Against.Null(appConfigurationContext, nameof(appConfigurationContext));
            _mediator = Guard.Against.Null(mediator, nameof(mediator));
            _performanceReporter = Guard.Against.Null(performanceReporter, nameof(performanceReporter));
            _windowService = Guard.Against.Null(windowService, nameof(windowService));
        }

        #endregion

        #region Private Methods (Commands)

        [RelayCommand]
        private Task InitializeAsync() {
            if (_initialized) { return Task.CompletedTask; }

            SearchHistory = [.. _appConfigurationContext.SearchHistory];

            _initialized = true;

            return Task.CompletedTask;
        }

        [RelayCommand]
        private async Task ExecuteSearchAsync(SearchTerm term) {
            UIHelper.Instance.ToggleBusyState();

            SearchResultEntryGroupCollectionResponse response;
            using (_performanceReporter.ReportExecutionTime($"{nameof(SearchViewModel)}.{nameof(ExecuteSearchAsync)}")) {
                response = await _mediator.Send(new ExecuteSearchRequest {
                    Query = term.Value
                });
            }

            await PubSubService
                .PublishAsync(new SnackbarNotification {
                    Title = "Pesquisa",
                    Message = response.Succeeded()
                        ? "Pesquisa executada com sucesso."
                        : response.Error,
                    Severity = response.Succeeded()
                        ? Severity.Success
                        : Severity.Error
                });

            if (response.Succeeded()) {
                AssertSearchHistory(term);
            }

            Result = [.. response.Value];
        }

        [RelayCommand]
        private Task VisualizeSearchResultEntryAsync(SearchResultEntryGroupDto entry) {
            _windowService
                .DisplaySearchResultWindow(entry);

            return Task.CompletedTask;
        }

        #endregion

        #region Private Methods

        private void AssertSearchHistory(SearchTerm term) {
            if (term.Suggested || SearchHistory.Contains(term.Value, StringComparer.InvariantCultureIgnoreCase)) {
                return;
            }

            SearchHistory.Add(term.Value);
            _appConfigurationContext.SearchHistory.Add(term.Value);
        }

        #endregion
    }
}
