using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace Nameless.InfoPhoenix.UI.Helpers {
    public sealed class UIHelper {
        #region Private Fields

        private bool _busy;

        #endregion

        #region Public Static Read-Only Properties

        public static UIHelper Instance { get; } = new();

        #endregion

        #region Static Constructors

        static UIHelper() { }

        #endregion

        #region Private Constructors

        private UIHelper() { }

        #endregion

        #region Private Methods

        private void SetBusyState(bool busy) {
            if (_busy == busy) { return; }

            _busy = busy;

            Mouse.OverrideCursor = busy ? Cursors.Wait : null;

            if (_busy) {
                _ = new DispatcherTimer(
                    interval: TimeSpan.FromSeconds(0),
                    priority: DispatcherPriority.ApplicationIdle,
                    callback: OnDispatcherTimerTick,
                    dispatcher: Application.Current.Dispatcher
                );
            }
        }

        private void OnDispatcherTimerTick(object? sender, EventArgs args) {
            if (sender is DispatcherTimer dispatcherTimer) {
                SetBusyState(false);
                dispatcherTimer.Stop();
            }
        }

        #endregion

        #region Public Methods

        public void ToggleBusyState()
            => SetBusyState(true);

        #endregion
    }
}
