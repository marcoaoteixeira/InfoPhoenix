namespace Nameless.InfoPhoenix.UI {
    public interface IViewModelAware<out TViewModel>
        where TViewModel : ViewModelBase {
        #region Properties

        public TViewModel ViewModel { get; }

        #endregion
    }
}
