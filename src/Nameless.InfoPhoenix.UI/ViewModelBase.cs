using CommunityToolkit.Mvvm.ComponentModel;
using Nameless.InfoPhoenix.Infrastructure;

namespace Nameless.InfoPhoenix.UI {
    public abstract class ViewModelBase : ObservableObject {
        #region Protected Properties

        protected IPubSubService PubSubService { get; }

        #endregion

        #region Protected Constructors

        protected ViewModelBase(IPubSubService pubSubService) {
            PubSubService = Guard.Against.Null(pubSubService, nameof(pubSubService));
        }

        #endregion
    }
}
