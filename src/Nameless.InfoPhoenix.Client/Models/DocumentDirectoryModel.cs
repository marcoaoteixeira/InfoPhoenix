using CommunityToolkit.Mvvm.ComponentModel;

namespace Nameless.InfoPhoenix.Client.Models {
    public partial class DocumentDirectoryModel : ObservableObject {
        #region Private Fields (Observable)

        [ObservableProperty]
        private string _label = string.Empty;

        [ObservableProperty]
        private string _directoryPath = string.Empty;

        [ObservableProperty]
        private int _order;

        [ObservableProperty]
        private DateTime? _lastIndexingTime;

        #endregion

        #region Public Properties

        public Guid ID { get; set; }

        #endregion
    }
}
